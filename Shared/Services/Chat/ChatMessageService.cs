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
            string content, string? guestToken, string? userId, CancellationToken cancellationToken = default)
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

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HubException("Nội dung tin nhắn không hợp lệ.");
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

            // =========================================================
            // 2. ENSURE CONTACT-INBOX
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
            // 3. RESOLVE CONVERSATION
            // =========================================================

            var conversationResult = await ResolveConversationForCustomerAsync(
                    contact, inbox, conversationId, inboxId, cancellationToken);

            var conversation = conversationResult.Conversation;

            var isNewConversation = conversationResult.IsNewConversation;

            // =========================================================
            // 4. CREATE MESSAGE
            // =========================================================

            var now = DateTime.UtcNow;

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

                Content = content,

                IsPrivate = false,

                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.ChatMessages.Add(message);

            // =========================================================
            // 5. UPDATE CONVERSATION
            // =========================================================

            conversation.LastMessageAt = now;
            conversation.UpdatedAt = now;

            if (string.IsNullOrWhiteSpace(conversation.Subject))
            {
                conversation.Subject = content.Length > 200 ? content[..200] : content;
            }

            // =========================================================
            // 6. RESOLVE STATUS
            // =========================================================

            var isAdminViewing = _chatPresenceService.IsConversationActive(conversation.Id);

            var statusChanged = false;

            if (!isAdminViewing && conversation.Status != ChatConversationStatus.Pending)
            {
                conversation.Status = ChatConversationStatus.Pending;

                statusChanged = true;
            }

            // =========================================================
            // 7. SAVE
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

                CreatedAt = message.CreatedAt
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
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            // =========================================================
            // 4. TOKEN ĐÃ BỊ REVOKE
            // =========================================================

            if (guestSession.IsRevoked)
            {
                throw new HubException(
                    "Phiên trò chuyện đã hết hiệu lực.");
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

            if (hasContactId && guestSession.ContactId != contactId!.Value)
            {
                throw new HubException("CHAT_SESSION_INVALID");
            }

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
