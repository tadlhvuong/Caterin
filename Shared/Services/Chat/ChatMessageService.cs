using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants.Core;
using Shared.Data.Context;
using Shared.Data.Entities.Chat;
using Shared.DTOs.Chat;
using Shared.Enums.Chat;
using Shared.Extensions;
using Shared.Interfaces.Chat;
using Shared.Interfaces.Notification;
using Shared.Requests.Chat;
using Shared.Responses;
using System.Security.Cryptography;

namespace Shared.Services.Chat
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly AppDbContext _dbContext;
        private readonly IChatPresenceService _chatPresenceService;
        private readonly IChatRealtimeNotifier _chatNotifier;
        private readonly IHubContext<ChatHub> _hubContext;

        private readonly ILogger<ChatMessageService> _logger;

        public ChatMessageService(AppDbContext dbContext, IHubContext<ChatHub> hubContext,IChatPresenceService chatPresenceService,
            IChatRealtimeNotifier chatNotifier, ILogger<ChatMessageService> logger)
        {
            _dbContext = dbContext;
            _chatPresenceService = chatPresenceService;
            _chatNotifier = chatNotifier;
            _hubContext = hubContext;

            _logger = logger;
        }
       
        public async Task<CustomerSendMessageResult> SendCustomerMessageAsync(long? conversationId, long? contactId, long inboxId,
            string content, string? guestToken, string? userId, string? uploadSessionToken, List<long>? uploadIds, CancellationToken cancellationToken = default)
        {

            var inbox = await _dbContext.ChatInboxes.FirstOrDefaultAsync(x => x.Id == inboxId, cancellationToken);

            if (inbox == null)
            {
                throw new HubException("Inbox không tồn tại.");
            }
            // =========================================================
            // 0. VALIDATE CONTENT
            // =========================================================

            content = content?.Trim() ?? string.Empty;

            var requestedUploadIds = uploadIds ?? new List<long>();

            if (requestedUploadIds.Count > 5)
                throw new HubException("Mỗi tin nhắn chỉ được đính kèm tối đa 5 file.");

            if (requestedUploadIds.Any(x => x <= 0))
                throw new HubException("Upload ID không hợp lệ.");

            if (requestedUploadIds.Count != requestedUploadIds.Distinct().Count())
                throw new HubException("Danh sách upload ID bị trùng.");

            if (string.IsNullOrWhiteSpace(content) && requestedUploadIds.Count == 0)
                throw new HubException("Tin nhắn phải có nội dung hoặc file đính kèm.");

            var isTemporaryGuestUpload = string.IsNullOrWhiteSpace(userId) && !contactId.HasValue &&
                string.IsNullOrWhiteSpace(guestToken) && requestedUploadIds.Count > 0;

            if (isTemporaryGuestUpload &&
                string.IsNullOrWhiteSpace(uploadSessionToken))
            {
                throw new HubException("CHAT_UPLOAD_SESSION_INVALID");
            }

            // Không cho dùng token upload tạm lẫn với danh tính đã tồn tại.
            if (!string.IsNullOrWhiteSpace(uploadSessionToken) &&
                !isTemporaryGuestUpload)
            {
                throw new HubException("CHAT_UPLOAD_SESSION_INVALID");
            }
            ChatContact contact;
            string? newGuestToken = null;
            bool isNewContact = false;

            // =========================================================
            // 1. RESOLVE IDENTITY
            // =========================================================

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // =====================================================
                // AUTHENTICATED CUSTOMER
                // =====================================================

                contact = await ResolveAuthenticatedContactAsync(userId, cancellationToken);

                /*
                 * Authenticated customer không dùng guest credential.
                 *
                 * Nếu client gửi guestToken cũ lên do localStorage chưa
                 * được clear thì cũng không sử dụng nó.
                 */
                guestToken = null;
            }
            else
            {
                // =====================================================
                // GUEST CUSTOMER
                // =====================================================

                var guestIdentity = await ResolveGuestIdentityAsync(contactId, guestToken, cancellationToken);

                contact = guestIdentity.Contact;
                newGuestToken = guestIdentity.GuestToken;
                isNewContact = guestIdentity.IsNewContact;

                /*
                 * Guest identity mới:
                 *
                 * Contact mới không được phép reuse conversation
                 * thuộc contact/session cũ.
                 */
                if (isNewContact)
                {
                    conversationId = null;
                }
            }

            // 2. XÁC ĐỊNH GUEST SESSION SỞ HỮU UPLOAD.
            // Không cho phép lấy upload của guest cũ khi đã đăng nhập,
            // hoặc lấy upload của session khác.
            long? guestSessionId = null;
            string? temporaryUploadTokenHash = null;
            var now = DateTime.UtcNow;

            if (requestedUploadIds.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    // Upload của khách đã đăng nhập sẽ được kiểm tra theo UserId.
                    if (!string.IsNullOrWhiteSpace(uploadSessionToken))
                    {
                        throw new HubException("CHAT_UPLOAD_SESSION_INVALID");
                    }
                }
                else if (isTemporaryGuestUpload)
                {
                    temporaryUploadTokenHash = Common.CommonHelper.Hash(uploadSessionToken!);

                    var temporaryUploads = await _dbContext.ChatPendingUploads
                        .AsNoTracking()
                        .Where(x =>
                            requestedUploadIds.Contains(x.Id) &&
                            x.Status == ChatPendingUploadStatus.Pending &&
                            x.ExpiresAt > now &&
                            x.UserId == null &&
                            x.GuestSessionId == null &&
                            x.UploadTokenHash == temporaryUploadTokenHash)
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken);

                    if (temporaryUploads.Count != requestedUploadIds.Count)
                    {
                        throw new HubException(
                            "Một hoặc nhiều file đã hết hạn hoặc không thuộc phiên upload này.");
                    }
                }
                else
                {
                    // Guest đã có danh tính: giữ nguyên cơ chế xác thực cũ.
                    if (string.IsNullOrWhiteSpace(guestToken))
                    {
                        throw new HubException("CHAT_SESSION_INVALID");
                    }

                    var tokenHash = Common.CommonHelper.Hash(guestToken);

                    guestSessionId = await _dbContext.ChatGuestSessions
                        .AsNoTracking()
                        .Where(x =>
                            x.TokenHash == tokenHash &&
                            x.ContactId == contact.Id &&
                            !x.IsRevoked &&
                            x.ExpiresAt > now)
                        .Select(x => (long?)x.Id)
                        .SingleOrDefaultAsync(cancellationToken);

                    if (!guestSessionId.HasValue)
                    {
                        throw new HubException("CHAT_SESSION_INVALID");
                    }
                }
            }
            // =========================================================
            // 3. ENSURE CONTACT-INBOX
            // =========================================================

            var contactInbox =
                await _dbContext.ChatContactInboxes.FirstOrDefaultAsync(
                        x => x.ContactId == contact.Id && x.InboxId == inboxId, cancellationToken);

            if (contactInbox == null)
            {
                contactInbox = new ChatContactInbox
                {
                    Contact = contact,
                    InboxId = inboxId,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ChatContactInboxes.Add(contactInbox);
            }

            // =========================================================
            // 4. RESOLVE CONVERSATION
            // =========================================================

            var conversationResult = await ResolveConversationForCustomerAsync(
                    contact, inbox, conversationId, inboxId, cancellationToken);

            var conversation = conversationResult.Conversation;

            var isNewConversation = conversationResult.IsNewConversation;

            // =========================================================
            // 4. CREATE MESSAGE
            // =========================================================

            var message = new ChatMessage
            {
                Conversation = conversation,
                Contact = contact,
                Inbox = inbox,
                UserId = userId,

                MessageType = ChatMessageType.Incoming,
                ContentType = ChatMessageContentType.Text,
                Status = ChatMessageStatus.Sent,
                SenderType = ChatSenderType.Contact,

                Content = string.IsNullOrWhiteSpace(content) ? null : content,
                IsPrivate = false,

                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.ChatMessages.Add(message);

            // 5. TRANSACTION + CLAIM UPLOADS.
            // Chỉ upload Pending, chưa hết hạn và thuộc đúng chủ sở hữu
            // mới được chuyển sang Attached.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var pendingUploads = new List<ChatPendingUpload>();

            foreach (var uploadId in requestedUploadIds)
            {
                int affectedRows;

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""chat_pending_upload""
           SET ""Status"" = {(int)ChatPendingUploadStatus.Attached},
               ""AttachedAt"" = {now},
               ""UploadTokenHash"" = NULL
           WHERE ""Id"" = {uploadId}
             AND ""Status"" = {(int)ChatPendingUploadStatus.Pending}
             AND ""ExpiresAt"" > {now}
             AND ""UserId"" = {userId}
             AND ""GuestSessionId"" IS NULL",
                        cancellationToken);
                }
                else if (isTemporaryGuestUpload)
                {
                    affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""chat_pending_upload""
           SET ""Status"" = {(int)ChatPendingUploadStatus.Attached},
               ""AttachedAt"" = {now},
               ""UploadTokenHash"" = NULL
           WHERE ""Id"" = {uploadId}
             AND ""Status"" = {(int)ChatPendingUploadStatus.Pending}
             AND ""ExpiresAt"" > {now}
             AND ""UserId"" IS NULL
             AND ""GuestSessionId"" IS NULL
             AND ""UploadTokenHash"" = {temporaryUploadTokenHash}",
                        cancellationToken);
                }
                else
                {
                    affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ""chat_pending_upload""
           SET ""Status"" = {(int)ChatPendingUploadStatus.Attached},
               ""AttachedAt"" = {now},
               ""UploadTokenHash"" = NULL
           WHERE ""Id"" = {uploadId}
             AND ""Status"" = {(int)ChatPendingUploadStatus.Pending}
             AND ""ExpiresAt"" > {now}
             AND ""UserId"" IS NULL
             AND ""GuestSessionId"" = {guestSessionId}",
                        cancellationToken);
                }

                if (affectedRows != 1)
                {
                    throw new HubException(
                        "Một hoặc nhiều file đã hết hạn, không hợp lệ hoặc đã được sử dụng.");
                }
            }

            if (requestedUploadIds.Count > 0)
            {
                // Đọc lại các upload sau khi claim thành công.
                // Chúng được truy vấn trong cùng transaction.
                pendingUploads = await _dbContext.ChatPendingUploads.Include(x => x.MediaFile).Where(x => requestedUploadIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);

                if (pendingUploads.Count != requestedUploadIds.Count || pendingUploads.Any(x => x.MediaFile == null))
                {
                    throw new HubException("Không thể đọc thông tin file đính kèm.");
                }

                var totalSize = pendingUploads.Sum(x => x.MediaFile.Size);

                if (totalSize > 25L * 1024 * 1024)
                    throw new HubException("Tổng dung lượng file vượt quá 25 MB.");

                foreach (var upload in pendingUploads)
                {
                    var media = upload.MediaFile;

                    var attachment = new ChatAttachment
                    {
                        Message = message,
                        MediaFile = media,
                        FileName = media.OriginalFileName ?? media.FileName,
                        ContentType = media.ContentType,
                        FileSize = media.Size
                    };

                    message.Attachments.Add(attachment);

                    // Navigation này giúp EF gán MessageId sau khi
                    // message được tạo trong cùng SaveChanges.
                    upload.Message = message;
                }

                // Enum hiện tại không có Mixed:
                // tất cả là ảnh => Image; có tài liệu => File.
                var allImages = pendingUploads.All(x => x.MediaFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

                message.ContentType = allImages ? ChatMessageContentType.Image : ChatMessageContentType.File;
            }

            // =========================================================
            // 6. UPDATE CONVERSATION
            // =========================================================

            conversation.LastMessageAt = now;
            conversation.UpdatedAt = now;

            if (string.IsNullOrWhiteSpace(conversation.Subject))
            {
                var subject = !string.IsNullOrWhiteSpace(content) ? content : pendingUploads.FirstOrDefault()?.MediaFile.OriginalFileName
                ?? pendingUploads.FirstOrDefault()?.MediaFile.FileName ?? "File đính kèm";

                conversation.Subject = subject.Length > 200 ? subject[..200] : subject;
            }

            // =========================================================
            // RESOLVE STATUS
            // =========================================================

            var isAdminViewing = _chatPresenceService.IsConversationActive(conversation.Id);

            var statusChanged = false;

            if (!isAdminViewing && conversation.Status != ChatConversationStatus.Pending)
            {
                conversation.Status = ChatConversationStatus.Pending;

                statusChanged = true;
            }

            // =========================================================
            // 7. SAVE MESSAGE + ATTACHMENTS + PENDING UPLOAD LINKS.
            //      Nếu SaveChanges thất bại, transaction rollback cả claim upload.
            // =========================================================

            /*
             * Một lần SaveChanges cho:
             *
             * - Contact mới
             * - ContactInbox mới
             * - Conversation mới
             * - Message mới
             * - Conversation status/update
             */
            await _dbContext.SaveChangesAsync(cancellationToken);
            // =========================================================
            // 8. DTO
            // =========================================================

            var dto = new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                InboxId = message.InboxId,
                ContactId = message.ContactId,
                SenderId = message.UserId,

                SenderName = contact.Name,
                SenderAvatar = contact.AvatarUrl,
                MessageType =  message.MessageType,
                ContentType = message.ContentType,
                Status = message.Status,
                SenderType = message.SenderType,
                Content = message.Content,
                IsPrivate = message.IsPrivate,
                CreatedAt = message.CreatedAt,
                Attachments = message.Attachments.Select(a => new ChatAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    Url = $"/chat/attachments/{a.Id}/download"
                }).ToList()
            };

            // =========================================================
            // 9. REALTIME STATUS
            // =========================================================

            if (statusChanged)
            {
                await _chatNotifier.NotifyConversationStatusUpdatedAsync(conversation.Id, conversation.InboxId,
                        conversation.Status, conversation.UpdatedAt);
            }

            // =========================================================
            // 10. REALTIME MESSAGE
            // =========================================================

            await _hubContext.Clients.Group(ChatHubGroups.Conversation(conversation.Id))
                .SendAsync(ChatHubEvents.MessageReceived, dto, cancellationToken);

            await _hubContext.Clients.Group(ChatHubGroups.Inbox(conversation.InboxId))
                .SendAsync(ChatHubEvents.MessageReceived, dto, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            // =========================================================
            // 11. RESULT
            // =========================================================

            return new CustomerSendMessageResult
            {
                Message = dto,

                ContactId = contact.Id,

                ConversationId = conversation.Id,

                Status = conversation.Status,

                /*
                 * Guest:
                 *   - first identity creation / token rotation
                 *     => plaintext token
                 *
                 * Authenticated:
                 *   => null
                 */
                GuestToken = newGuestToken,

                IsNewContact = isNewContact,

                IsNewConversation = isNewConversation
            };
        }

        public async Task<ChatMessageDto> SendAdminMessageAsync(long conversationId, string userId,
            string content, CancellationToken cancellationToken = default)
        {
            content = content.Trim();

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tin nhắn không được rỗng.");

            var conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

            if (conversation == null)
                throw new InvalidOperationException("Conversation không tồn tại.");

            if (conversation.Status == ChatConversationStatus.Resolved ||  conversation.Status == ChatConversationStatus.Closed)
            {
                throw new InvalidOperationException("Conversation đã kết thúc.");
            }
            var now = DateTime.UtcNow;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,

                InboxId = conversation.InboxId,

                ContactId = conversation.ContactId, 

                UserId = userId,

                MessageType = ChatMessageType.Outgoing,

                ContentType = ChatMessageContentType.Text,

                Status = ChatMessageStatus.Sent,

                SenderType = ChatSenderType.Admin,

                Content = content,

                IsPrivate = false,

                CreatedAt = now,

                UpdatedAt = now
            };

            _dbContext.ChatMessages.Add(message);

            conversation.LastMessageAt = now;
            conversation.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("Admin không tồn tại.");

            var dto = new ChatMessageDto
            {
                Id = message.Id,

                ConversationId = message.ConversationId,

                InboxId = message.InboxId,

                ContactId = message.ContactId,

                SenderId = userId,

                SenderName = user?.UserName,

                SenderAvatar = user?.Avatar,

                MessageType = message.MessageType,

                ContentType = message.ContentType,

                Status = message.Status,

                SenderType = message.SenderType,

                Content = message.Content,

                IsPrivate = message.IsPrivate,

                CreatedAt = message.CreatedAt
            };

            await _hubContext.Clients.Group(ChatHubGroups.Conversation(conversation.Id)).SendAsync(ChatHubEvents.MessageReceived, dto, cancellationToken);

            await _hubContext.Clients.Group(ChatHubGroups.Inbox(conversation.InboxId)).SendAsync(ChatHubEvents.ConversationUpdated, dto, cancellationToken);

            return dto;
        }
        
        public async Task<object> GetAdminMessagesAsync(long conversationId, int limit, long? before, CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            var query = _dbContext.ChatMessages.AsNoTracking().Where(x => x.ConversationId == conversationId);

            if (before.HasValue)
            {
                query = query.Where(x => x.Id < before.Value);
            }

            var messages = await query.OrderByDescending(x => x.Id).Take(limit + 1).Select(x => new ChatMessageDto
                {
                    Id = x.Id,
                    ConversationId = x.ConversationId,
                    InboxId = x.InboxId,

                    ContactId = x.ContactId,
                    SenderId = x.UserId,

                    SenderName = x.SenderType == ChatSenderType.Contact ? x.Contact!.Name : x.User!.UserName,

                    SenderAvatar = x.SenderType == ChatSenderType.Contact ? x.Contact!.AvatarUrl : x.User!.Avatar,

                    MessageType = x.MessageType,
                    ContentType = x.ContentType,
                    Status = x.Status,
                    SenderType = x.SenderType,

                    Content = x.Content,
                    IsPrivate = x.IsPrivate,
                    CreatedAt = x.CreatedAt
                }).ToListAsync(cancellationToken);

            var hasMore = messages.Count > limit;

            if (hasMore)
            {
                messages.RemoveAt(messages.Count - 1);
            }

            messages.Reverse();

            return new
            {
                Items = messages,
                HasMore = hasMore,
                OldestMessageId = messages.Count > 0 ? messages[0].Id : (long?)null,
                NewestMessageId = messages.Count > 0 ? messages[^1].Id : (long?)null
            };
        }

        public async Task<object> GetCustomerMessagesAsync(long conversationId, int limit, long? before, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ChatMessages.AsNoTracking().Where(x => x.ConversationId == conversationId && !x.IsPrivate);

            // Load các message cũ hơn message hiện tại
            if (before.HasValue)
            {
                query = query.Where(x => x.Id < before.Value);
            }

            // Lấy thêm 1 message để xác định còn dữ liệu hay không
            var messages = await query.OrderByDescending(x => x.Id).Take(limit + 1).Select(x => new ChatMessageDto
                {
                    Id = x.Id,
                    ConversationId = x.ConversationId,
                    InboxId = x.InboxId,
                    ContactId = x.ContactId,
                    SenderId = x.UserId,

                    SenderName = x.SenderType == ChatSenderType.Contact ? x.Contact!.Name : x.User!.UserName,

                    SenderAvatar = x.SenderType == ChatSenderType.Contact ? x.Contact!.AvatarUrl : x.User!.Avatar,

                    MessageType = x.MessageType,
                    ContentType = x.ContentType,
                    Status = x.Status,
                    SenderType = x.SenderType,
                    Content = x.Content,
                    IsPrivate = x.IsPrivate,
                    CreatedAt = x.CreatedAt,

                    // Lấy file đính kèm của từng message.
                    Attachments = x.Attachments
                    .Select(a => new ChatAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSize = a.FileSize,
                        Url = "/chat/attachments/" + a.Id + "/download"
                    }).ToList()
            }).ToListAsync(cancellationToken);

            var hasMore = messages.Count > limit;

            if (hasMore)
            {
                messages.RemoveAt(messages.Count - 1);
            }

            // API trả về theo thứ tự cũ -> mới
            messages.Reverse();

            return new
            {
                Items = messages,
                HasMore = hasMore
            };
        }

        
        private async Task<ChatContact> ResolveAuthenticatedContactAsync(string userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Không xác định được người dùng.");
            }

            userId = userId.Trim();

            // =========================================================
            // 1. Tìm canonical ChatContact của authenticated customer
            // =========================================================

            var contact = await _dbContext.ChatContacts .FirstOrDefaultAsync(x => x.UserId == userId, ct);

            if (contact != null)
            {
                return contact;
            }

            // =========================================================
            // 2. Chưa có Contact
            // => tạo canonical Contact mới
            // =========================================================

            contact = new ChatContact
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ChatContacts.Add(contact);

            /*
             * Không SaveChanges ở đây.
             *
             * SendCustomerMessageAsync() sẽ SaveChanges một lần
             * sau khi tạo ContactInbox / Conversation / Message.
             */
            return contact;
        }

        private async Task<GuestIdentityResult> ResolveGuestIdentityAsync(long? contactId,
    string? guestToken,
    CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var hasContactId = contactId.HasValue && contactId.Value > 0;

            var hasGuestToken = !string.IsNullOrWhiteSpace(guestToken);

            // =========================================================
            // 1. FIRST GUEST MESSAGE
            //
            // Không có Contact + không có Token
            // => tạo Guest Contact + GuestSession mới
            // =========================================================

            if (!hasContactId && !hasGuestToken)
            {
                var contact = new ChatContact
                {
                    UserId = null,
                    CreatedAt = now
                };

                _dbContext.ChatContacts.Add(contact);

                var token = Common.CommonHelper.GenerateSecureToken(32);

                var hashToken = Common.CommonHelper.Hash(token);

                var session = new ChatGuestSession
                {
                    Contact = contact,
                    TokenHash = hashToken,
                    CreatedAt = now,
                    LastSeenAt = now,
                    ExpiresAt = now.AddDays(7),
                    IsRevoked = false
                };

                _dbContext.ChatGuestSessions.Add(session);

                return new GuestIdentityResult
                {
                    Contact = contact,
                    GuestToken = token,
                    IsNewContact = true
                };
            }

            // =========================================================
            // 2. TOKEN KHÔNG CÓ
            //
            // Có ContactId nhưng không có GuestToken
            // => không đủ credential để xác thực Guest Contact.
            // =========================================================

            if (hasContactId && !hasGuestToken)
            {
                throw new HubException("CHAT_SESSION_INVALID");
            }

            // =========================================================
            // 3. TOKEN CÓ NHƯNG KHÔNG CÓ CONTACT ID
            //
            // Token là credential.
            // Có thể tìm Contact từ GuestSession.
            // =========================================================

            var tokenHash =
                Common.CommonHelper.Hash(guestToken!);

            var guestSession =
                await _dbContext.ChatGuestSessions
                    .Include(x => x.Contact)
                    .FirstOrDefaultAsync(
                        x => x.TokenHash == tokenHash,
                        ct);

            // Không tìm thấy token
            if (guestSession == null)
            {
                throw new HubException("CHAT_SESSION_INVALID");
            }
            // Kiểm tra contact trước cả khi xử lý token hết hạn.
            if (hasContactId && guestSession.ContactId != contactId!.Value)
            {
                throw new HubException("CHAT_SESSION_INVALID");
            }
            if (guestSession.IsRevoked)
            {
                throw new HubException("CHAT_SESSION_INVALID");
            }

            // =========================================================
            // 5. TOKEN HẾT HẠN
            //
            // Không được dùng token hết hạn để tiếp tục Conversation.
            //
            // Có thể tạo session/token mới cho cùng Contact nếu đúng
            // rule expired-session của bạn.
            // =========================================================

            if (guestSession.ExpiresAt <= now)
            {
                guestSession.IsRevoked = true;
                guestSession.RevokedAt = now;
                guestSession.RevokedReason = "Revoled id: " + guestSession.Id + " invalid: ";

                var contact = guestSession.Contact;

                var newToken = Common.CommonHelper.GenerateSecureToken(32);

                var newTokenHash = Common.CommonHelper.Hash(newToken);

                var newSession = new ChatGuestSession
                {
                    Contact = contact,
                    TokenHash = newTokenHash,
                    CreatedAt = now,
                    LastSeenAt = now,
                    ExpiresAt = now.AddDays(7),
                    IsRevoked = false
                };

                _dbContext.ChatGuestSessions.Add(newSession);

                return new GuestIdentityResult
                {
                    Contact = contact,
                    GuestToken = newToken,
                    IsNewContact = false
                };
            }

            // =========================================================
            // 6. TOKEN HỢP LỆ NHƯNG CONTACT ID KHÔNG KHỚP
            //
            // Đây là trường hợp QUAN TRỌNG:
            //
            // request:
            //   contactId = C1
            //   guestToken = T2
            //
            // nhưng T2 thuộc C2.
            //
            // KHÔNG được:
            //   - refresh T2 cho C1
            //   - dùng Conversation của C1
            //   - tự sửa ContactId
            // =========================================================
///FIX ĐƯA LÊN TRÊN ĐỢI SAU NÀY TESTCASE NẾU ĐÚNG THÌ XÓA ĐI
            //if (hasContactId && guestSession.ContactId != contactId!.Value)
            //{
            //    throw new HubException("CHAT_SESSION_INVALID");
            //}

            // =========================================================
            // 7. TOKEN HỢP LỆ
            // =========================================================

            guestSession.LastSeenAt = now;

            return new GuestIdentityResult
            {
                Contact = guestSession.Contact,
                GuestToken = null,
                IsNewContact = false
            };
        }

        private async Task<(ChatConversation Conversation, bool IsNewConversation)> ResolveConversationForCustomerAsync(
        ChatContact contact, ChatInbox inbox, long? conversationId, long inboxId, CancellationToken ct)
        {
            // =========================================================
            // 1. Nếu client có conversationId
            // =========================================================

            if (conversationId.HasValue)
            {
                var existing = await _dbContext.ChatConversations.FirstOrDefaultAsync(
                            x => x.Id == conversationId.Value && x.ContactId == contact.Id && x.InboxId == inboxId, ct);

                // Conversation không tồn tại hoặc không thuộc Contact/Inbox
                if (existing == null)
                {
                    throw new HubException("CHAT_SESSION_INVALID");
                }

                // =====================================================
                // 2. Conversation đang Active
                // => tiếp tục conversation này
                // =====================================================

                if (existing.Status == ChatConversationStatus.Open || existing.Status == ChatConversationStatus.Pending)
                {
                    return (existing, false);
                }

                // =====================================================
                // 3. Conversation đã Resolved / Closed
                // => KHÔNG reopen
                // => tìm conversation Active khác của Contact
                // =====================================================

                var activeConversation = await _dbContext.ChatConversations.Where(x =>
                            x.ContactId == contact.Id && x.InboxId == inboxId &&
                            (
                                x.Status == ChatConversationStatus.Open ||
                                x.Status == ChatConversationStatus.Pending
                            ))
                        .OrderByDescending(x => x.LastMessageAt).FirstOrDefaultAsync(ct);

                if (activeConversation != null)
                {
                    return (activeConversation, false);
                }

                // Không còn conversation Active
                // => tạo conversation mới
                return CreateNewConversation(contact, inbox, existing.Priority);
            }

            // =========================================================
            // 4. Client KHÔNG gửi conversationId
            //
            // Đây chính là case:
            //
            // logout → login → gửi message
            //
            // => phải tìm conversation Active của Contact
            // =========================================================

            var currentConversation = await _dbContext.ChatConversations.Where(x =>
                        x.ContactId == contact.Id && x.InboxId == inboxId &&
                        (
                            x.Status == ChatConversationStatus.Open ||
                            x.Status == ChatConversationStatus.Pending
                        ))
                    .OrderByDescending(x => x.LastMessageAt).FirstOrDefaultAsync(ct);

            if (currentConversation != null)
            {
                return (currentConversation, false);
            }

            // =========================================================
            // 5. Không có conversation Active
            // => tạo conversation mới
            // =========================================================

            return CreateNewConversation(contact, inbox, ChatConversationPriority.Low);
        }

        private (ChatConversation Conversation, bool IsNewConversation) CreateNewConversation(ChatContact contact, ChatInbox inbox,
            ChatConversationPriority priority)
        {
            var now = DateTime.UtcNow;

            var conversation = new ChatConversation
            {
                Contact = contact,
                Inbox = inbox,

                Status = ChatConversationStatus.Pending,
                Priority = priority,

                LastMessageAt = now,
                CreatedAt = now
            };

            _dbContext.ChatConversations.Add(conversation);

            return (conversation, true);
        }
    }
}
