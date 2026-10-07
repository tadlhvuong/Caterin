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
        public async Task<StartChatResponse> StartClientConversationAsync(
     StartChatRequest request,
     string? userId,
     CancellationToken cancellationToken = default)
        {
            var inbox =
                await _dbContext.ChatInboxes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == request.InboxId &&
                            x.IsActive,
                        cancellationToken);

            if (inbox == null)
            {
                throw new InvalidOperationException(
                    "Chat inbox không tồn tại hoặc đã bị tắt.");
            }

            // =========================================================
            // AUTHENTICATED
            // =========================================================

            if (!string.IsNullOrWhiteSpace(userId))
            {
                userId = userId.Trim();

                ChatContact? contact = null;

                // -----------------------------------------------------
                // 1. Guest -> Login
                //    Claim hoặc merge Guest Contact
                // -----------------------------------------------------

                if (request.ContactId.HasValue &&
                    !string.IsNullOrWhiteSpace(request.GuestToken))
                {
                    contact =
                        await ClaimOrMergeGuestContactAsync(
                            request.ContactId.Value,
                            request.GuestToken,
                            userId,
                            cancellationToken);
                }
                else
                {
                    // -------------------------------------------------
                    // 2. Auth-first
                    //
                    // Không có guest identity.
                    // Không tạo Contact ở /start.
                    // -------------------------------------------------

                    contact =
                        await _dbContext.ChatContacts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.UserId == userId &&
                                    !x.IsMerged,
                                cancellationToken);
                }

                long? currentConversationId = null;

                // =====================================================
                // 3. Nếu có Contact + Conversation hiện tại
                //    thì kiểm tra conversation
                // =====================================================

                if (contact != null)
                {
                    var isGuestConversationCurrent = false;
                    // Có conversationId từ guest session
                    if (request.ConversationId.HasValue)
                    {
                        var currentConversation =
                            await _dbContext.ChatConversations
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.Id == request.ConversationId.Value &&
                                        x.ContactId == contact.Id &&
                                        x.InboxId == inbox.Id,
                                    cancellationToken);

                        if (currentConversation == null)
                            throw new HubException("CHAT_SESSION_INVALID");

                        if (currentConversation.Status ==
                                ChatConversationStatus.Open ||
                            currentConversation.Status ==
                                ChatConversationStatus.Pending)
                        {
                            currentConversationId =
                                currentConversation.Id;

                            isGuestConversationCurrent = true;
                        }
                    }

                    // Không có current conversation
                    // → tìm conversation active của user
                    if (!currentConversationId.HasValue)
                    {
                        currentConversationId =
                            await _dbContext.ChatConversations
                                .Where(x =>
                                    x.ContactId == contact.Id &&
                                    x.InboxId == inbox.Id &&
                                    (
                                        x.Status == ChatConversationStatus.Open ||
                                        x.Status == ChatConversationStatus.Pending
                                    ))
                                .OrderByDescending(x => x.LastMessageAt)
                                .Select(x => (long?)x.Id)
                                .FirstOrDefaultAsync(
                                    cancellationToken);
                    }

                    var wasGuestContactMerged =
    request.ContactId.HasValue &&
    contact.Id != request.ContactId.Value;

                    if (wasGuestContactMerged && isGuestConversationCurrent &&
    currentConversationId.HasValue)
                    {
                        await ReconcileActiveConversationsAfterGuestLoginAsync(
                            contact.Id,
                            inbox.Id,
                            currentConversationId.Value,
                            userId,
                            cancellationToken);
                    }
                }

                return new StartChatResponse
                {
                    ContactId = contact?.Id,

                    InboxId = inbox.Id,

                    ConversationId = currentConversationId,

                    ContactName = contact?.Name,

                    ContactAvatar = contact?.AvatarUrl
                };
            }

            // =========================================================
            // GUEST
            //
            // /start không tạo Contact.
            // =========================================================

            return new StartChatResponse
            {
                ContactId = null,
                InboxId = inbox.Id,
                ConversationId = null,
                ContactName = null,
                ContactAvatar = null
            };
        }

        public async Task<ChatConversationDto> StartAdminConversationAsync(
    long inboxId,
    long contactId,
    CancellationToken cancellationToken = default)
        {
            var inbox = await _dbContext.ChatInboxes
                .FirstOrDefaultAsync(
                    x => x.Id == inboxId && x.IsActive,
                    cancellationToken);

            if (inbox == null)
            {
                throw new InvalidOperationException(
                    "Chat inbox không tồn tại hoặc đã bị tắt.");
            }

            var contact = await _dbContext.ChatContacts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == contactId,
                    cancellationToken);

            if (contact == null)
            {
                throw new InvalidOperationException(
                    "Contact không tồn tại.");
            }

            var now = DateTime.UtcNow;

            // =========================================================
            // 1. CONTACT - INBOX
            // =========================================================

            var contactInbox =
                await _dbContext.ChatContactInboxes
                    .FirstOrDefaultAsync(
                        x =>
                            x.ContactId == contactId &&
                            x.InboxId == inboxId,
                        cancellationToken);

            if (contactInbox == null)
            {
                contactInbox = new ChatContactInbox
                {
                    ContactId = contactId,
                    InboxId = inboxId,
                    CreatedAt = now
                };

                _dbContext.ChatContactInboxes.Add(contactInbox);
            }

            // =========================================================
            // 2. FIND OPEN CONVERSATION
            // =========================================================

            var conversation =
                await _dbContext.ChatConversations
                    .FirstOrDefaultAsync(
                        x =>
                            x.InboxId == inboxId &&
                            x.ContactId == contactId &&
                            x.Status != ChatConversationStatus.Closed &&
                            x.Status != ChatConversationStatus.Resolved,
                        cancellationToken);
            var isNewConversation = false;

            // =========================================================
            // 3. CREATE CONVERSATION
            // =========================================================

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    InboxId = inboxId,
                    ContactId = contactId,
                    Status = ChatConversationStatus.Open,
                    Priority = ChatConversationPriority.Low,
                    LastMessageAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.ChatConversations.Add(conversation);
                isNewConversation = true;
            }
            else
            {
                conversation.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // =========================================================
            // 4. RESULT
            // =========================================================

            var conversationDto = new ChatConversationDto
            {
                Id = conversation.Id,
                InboxId = conversation.InboxId,
                ContactId = conversation.ContactId,

                ContactName = contact.Name,
                ContactAvatar = contact.AvatarUrl,

                AssignedUserId = conversation.AssignedUserId,

                Status = conversation.Status,
                Priority = conversation.Priority,

                Subject = conversation.Subject,

                LastMessageAt = conversation.LastMessageAt,
                CreatedAt = conversation.CreatedAt
            };

            // =========================================================
            // 5. REALTIME - NEW CONVERSATION
            // =========================================================

            if (isNewConversation)
            {
                await _hubContext.Clients
                    .Group(
                        ChatHubGroups.Inbox(conversation.InboxId))
                    .SendAsync(
                        ChatHubEvents.ConversationCreated,
                        conversationDto,
                        cancellationToken);
            }


            return conversationDto;
        }
        public async Task<CustomerSendMessageResult> SendCustomerMessageAsync(
    long? conversationId,
    long? contactId,
    long inboxId,
    string content,
    string? guestToken,
    string? userId,
    CancellationToken cancellationToken = default)
        {

            var inbox =
    await _dbContext.ChatInboxes
        .FirstOrDefaultAsync(
            x => x.Id == inboxId,
            cancellationToken);

            if (inbox == null)
            {
                throw new HubException(
                    "Inbox không tồn tại.");
            }
            // =========================================================
            // 0. VALIDATE CONTENT
            // =========================================================

            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HubException(
                    "Nội dung tin nhắn không hợp lệ.");
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

                contact =
                    await ResolveAuthenticatedContactAsync(
                        userId,
                        cancellationToken);

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

                var guestIdentity =
                    await ResolveGuestIdentityAsync(
                        contactId,
                        guestToken,
                        cancellationToken);

                contact =
                    guestIdentity.Contact;

                newGuestToken =
                    guestIdentity.GuestToken;

                isNewContact =
                    guestIdentity.IsNewContact;

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
                await _dbContext.ChatContactInboxes
                    .FirstOrDefaultAsync(
                        x =>
                            x.ContactId == contact.Id &&
                            x.InboxId == inboxId,
                        cancellationToken);

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

            var conversationResult =
                await ResolveConversationForCustomerAsync(
                    contact, inbox,
                    conversationId,
                    inboxId,
                    cancellationToken);

            var conversation =
                conversationResult.Conversation;

            var isNewConversation =
                conversationResult.IsNewConversation;

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

                MessageType =
                    ChatMessageType.Incoming,

                ContentType =
                    ChatMessageContentType.Text,

                Status =
                    ChatMessageStatus.Sent,

                SenderType =
                    ChatSenderType.Contact,

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
                conversation.Subject =
                    content.Length > 200
                        ? content[..200]
                        : content;
            }

            // =========================================================
            // 6. RESOLVE STATUS
            // =========================================================

            var isAdminViewing =
                _chatPresenceService
                    .IsConversationActive(
                        conversation.Id);

            var statusChanged = false;

            if (!isAdminViewing &&
                conversation.Status !=
                    ChatConversationStatus.Pending)
            {
                conversation.Status =
                    ChatConversationStatus.Pending;

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
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // 8. DTO
            // =========================================================

            var dto = new ChatMessageDto
            {
                Id = message.Id,

                ConversationId =
                    message.ConversationId,

                InboxId =
                    message.InboxId,

                ContactId = message.ContactId,

                SenderId =
                    message.UserId,

                SenderName =
                    contact.Name,

                SenderAvatar =
                    contact.AvatarUrl,

                MessageType =
                    message.MessageType,

                ContentType =
                    message.ContentType,

                Status =
                    message.Status,

                SenderType =
                    message.SenderType,

                Content =
                    message.Content,

                IsPrivate =
                    message.IsPrivate,

                CreatedAt =
                    message.CreatedAt
            };

            // =========================================================
            // 9. REALTIME STATUS
            // =========================================================

            if (statusChanged)
            {
                await _chatNotifier
                    .NotifyConversationStatusUpdatedAsync(
                        conversation.Id,
                        conversation.InboxId,
                        conversation.Status,
                        conversation.UpdatedAt);
            }

            // =========================================================
            // 10. REALTIME MESSAGE
            // =========================================================

            await _hubContext.Clients
                .Group(
                    ChatHubGroups.Conversation(
                        conversation.Id))
                .SendAsync(
                    ChatHubEvents.MessageReceived,
                    dto,
                    cancellationToken);

            await _hubContext.Clients
                .Group(
                    ChatHubGroups.Inbox(
                        conversation.InboxId))
                .SendAsync(
                    ChatHubEvents.MessageReceived,
                    dto,
                    cancellationToken);

            // =========================================================
            // 11. RESULT
            // =========================================================

            return new CustomerSendMessageResult
            {
                Message =
                    dto,

                ContactId =
                    contact.Id,

                ConversationId =
                    conversation.Id,

                Status =
                    conversation.Status,

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
                throw new InvalidOperationException(
                    "Conversation không tồn tại.");

            if (conversation.Status == ChatConversationStatus.Resolved ||  conversation.Status == ChatConversationStatus.Closed)
            {
                throw new InvalidOperationException(
                    "Conversation đã kết thúc.");
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
            var user = await _dbContext.Users
    .FirstOrDefaultAsync(
        x => x.Id == userId,
        cancellationToken);
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

            await _hubContext.Clients.Group(ChatHubGroups.Conversation(conversation.Id))
            .SendAsync(ChatHubEvents.MessageReceived, dto, cancellationToken);

            await _hubContext.Clients.Group(ChatHubGroups.Inbox(conversation.InboxId))
            .SendAsync(ChatHubEvents.ConversationUpdated, dto, cancellationToken);

            return dto;
        }


        public async Task<ChatConversationDto?> GetConversationAsync(long conversationId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new ChatConversationDto
                {
                    Id = x.Id,
                    InboxId = x.InboxId,
                    ContactId = x.ContactId,

                    ContactName = x.Contact.Name,
                    ContactAvatar = x.Contact.AvatarUrl,

                    AssignedUserId = x.AssignedUserId,

                    Status = x.Status,
                    Priority = x.Priority,

                    Subject = x.Subject,

                    LastMessageAt = x.LastMessageAt,

                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<object> GetConversationListAsync(long? inboxId,
    ChatConversationStatus? status, string? search, string? currentUserId, long? labelId, int limit = 30, DateTime? beforeLastMessageAt = null,
        long? beforeId = null,
    CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            var query = _dbContext.ChatConversations.AsNoTracking().AsQueryable();

            if (inboxId.HasValue && inboxId.Value > 0)
            {
                query = query.Where(x => x.InboxId == inboxId.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }
            if (labelId.HasValue)
            {
                query = query.Where(x =>
                       _dbContext.ChatConversationLabels.Any(cl =>
                           cl.ConversationId == x.Id &&
                           cl.LabelId == labelId.Value));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Contact.Name.Contains(search) ||
                    x.Contact.Email.Contains(search) ||
                    x.Contact.Phone.Contains(search) || (x.Subject != null && x.Subject.Contains(search)) ||
                     x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault().Contains(search));
            }
            // Cursor:
            // LastMessageAt nhỏ hơn
            // hoặc cùng LastMessageAt nhưng Id nhỏ hơn
            if (beforeLastMessageAt.HasValue && beforeId.HasValue)
            {
                query = query.Where(x =>
                    x.LastMessageAt < beforeLastMessageAt.Value ||
                    (
                        x.LastMessageAt == beforeLastMessageAt.Value &&
                        x.Id < beforeId.Value
                    ));
            }

            var conversations = await query.OrderByDescending(x => x.LastMessageAt).ThenByDescending(x => x.Id).Take(limit + 1)
                .Select(x => new ChatConversationListItemDto
                {
                    Id = x.Id,
                    InboxId = x.InboxId,
                    ContactId = x.ContactId,

                    ContactName = x.Contact.Name,
                    ContactAvatarUrl = x.Contact.AvatarUrl,

                    Status = x.Status,
                    Priority = x.Priority,

                    Subject = x.Subject,
                    LastMessageAt = x.LastMessageAt,

                    AssignedUserId = x.AssignedUserId,
                    TeamId = x.TeamId,

                    LastMessage = x.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),

                    UnreadCount = x.Messages.Count(m =>
                        m.SenderType == ChatSenderType.Contact &&
                        m.Status != ChatMessageStatus.Read),

                    Labels = x.Labels
                        .Select(l => new ChatContactLabelDto
                        {
                            Id = l.Label.Id,
                            Name = l.Label.Name,
                            Color = l.Label.Color
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var hasMore = conversations.Count > limit;

            if (hasMore)
            {
                conversations.RemoveAt(conversations.Count - 1);
            }

            return new ChatConversationListResponse
            {
                Items = conversations,
                HasMore = hasMore,

                OldestLastMessageAt = conversations.Count > 0 ? conversations[^1].LastMessageAt : null,

                OldestId = conversations.Count > 0 ? conversations[^1].Id : null
            };
        }

        public async Task<ChatConversationCountsDto> GetConversationCountsAsync(long? inboxId, string? search = null, string? assignedUserId = null, long? labelId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ChatConversations.AsNoTracking().AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Contact.Name.Contains(search) ||
                    x.Contact.Email.Contains(search) ||
                    x.Contact.Phone.Contains(search) ||
                    (x.Subject != null && x.Subject.Contains(search)) ||
                     x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault().Contains(search));
            }

            // Assigned user
            if (!string.IsNullOrWhiteSpace(assignedUserId))
            {
                query = query.Where(x =>
                    x.AssignedUserId == assignedUserId);
            }

            // Label
            if (labelId.HasValue)
            {
                query = query.Where(x =>
                    x.Labels.Any(l => l.LabelId == labelId.Value));
            }

            var all = await query.CountAsync(cancellationToken);

            var website = await query.CountAsync(x => x.InboxId == 1, cancellationToken);

            var facebook = await query.CountAsync(x => x.InboxId == 2, cancellationToken);

            var open = await query.CountAsync(
                x => x.Status == ChatConversationStatus.Open,
                cancellationToken);

            var pending = await query.CountAsync(
                x => x.Status == ChatConversationStatus.Pending,
                cancellationToken);

            var resolved = await query.CountAsync(
                x => x.Status == ChatConversationStatus.Resolved,
                cancellationToken);

            return new ChatConversationCountsDto
            {
                All = all,
                Website = website,
                Facebook = facebook,
                Open = open,
                Pending = pending,
                Resolved = resolved
            };
        }
        public async Task<bool> CanAccessConversationAsync(long conversationId, long? contactId,
            string? userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == conversationId,
                    cancellationToken);

            if (conversation == null)
                return false;

            // =========================
            // ADMIN
            // =========================
            if (isAdmin)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                // Phase 1:
                // Admin đã đăng nhập thì được phép truy cập.
                //
                // Sau khi nối với Permission system:
                // Chat.View / Chat.Reply
                // sẽ kiểm tra ở đây.

                return true;
            }

            // =========================
            // CUSTOMER / GUEST
            // =========================

            if (!contactId.HasValue)
                return false;

            // Conversation phải thuộc Contact
            if (conversation.ContactId != contactId.Value)
                return false;

            // Nếu contact là user đã đăng nhập
            // thì kiểm tra thêm UserId.
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var contact = await _dbContext.ChatContacts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == contactId.Value,
                        cancellationToken);

                if (contact == null)
                    return false;

                // Contact đã liên kết AppUser khác
                if (!string.IsNullOrWhiteSpace(contact.UserId) &&
                    contact.UserId != userId)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> CanCustomerAccessConversationAsync(
     long conversationId,
     long? contactId,
     string? userId,
     string? guestToken,
     CancellationToken cancellationToken = default)
        {
            var conversation =
                await _dbContext.ChatConversations
                    .AsNoTracking()
                    .Where(x => x.Id == conversationId)
                    .Select(x => new
                    {
                        x.ContactId,
                        ContactUserId = x.Contact.UserId,
                        ContactIsMerged = x.Contact.IsMerged
                    })
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (conversation == null)
            {
                return false;
            }

            // =========================================================
            // AUTHENTICATED CUSTOMER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // Authenticated customer không cần GuestToken.
                //
                // Contact phải là canonical Contact của UserId.
                if (!contactId.HasValue)
                {
                    return false;
                }

                if (conversation.ContactId != contactId.Value)
                {
                    return false;
                }

                if (conversation.ContactIsMerged)
                {
                    return false;
                }

                return conversation.ContactUserId == userId;
            }

            // =========================================================
            // GUEST
            // =========================================================

            if (!contactId.HasValue ||
                string.IsNullOrWhiteSpace(guestToken))
            {
                return false;
            }

            if (conversation.ContactId != contactId.Value)
            {
                return false;
            }

            if (conversation.ContactIsMerged)
            {
                return false;
            }

            var tokenHash =
                Common.CommonHelper.Hash(guestToken);

            var now = DateTime.UtcNow;

            return await _dbContext.ChatGuestSessions
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ContactId == contactId.Value &&
                        x.TokenHash == tokenHash &&
                        !x.IsRevoked &&
                        x.ExpiresAt > now,
                    cancellationToken);
        }
        public async Task<ChatInbox?> GetDefaultInboxAsync(
    CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatInboxes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<ChatConversationDetailDto?> GetConversationDetailAsync(
    long conversationId,
    CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new ChatConversationDetailDto
                {
                    Id = x.Id,
                    InboxId = x.InboxId,
                    ContactId = x.ContactId,

                    ContactName = x.Contact.Name,
                    ContactEmail = x.Contact.Email,
                    ContactPhone = x.Contact.Phone,
                    ContactAvatarUrl = x.Contact.AvatarUrl,

                    AssignedUserId = x.AssignedUserId,
                    AssignedUserName = x.AssignedUser != null
                        ? x.AssignedUser.UserName
                        : null,

                    TeamId = x.TeamId,
                    TeamName = x.Team != null
                        ? x.Team.Name
                        : null,

                    Subject = x.Subject,

                    Status = x.Status,
                    Priority = x.Priority,

                    LastMessageAt = x.LastMessageAt,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<ChatConversationContactDto?> GetConversationContactAsync(
     long conversationId,
     CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new
                {
                    ConversationId = x.Id,
                    ContactId = x.ContactId,

                    Contact = new
                    {
                        x.Contact.Id,
                        x.Contact.UserId,
                        x.Contact.Name,
                        x.Contact.Email,
                        x.Contact.Phone,
                        x.Contact.AvatarUrl,
                        x.Contact.CustomAttributesJson,
                        x.Contact.CreatedAt
                    }
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversation == null)
            {
                return null;
            }

            var result = new ChatConversationContactDto
            {
                ConversationId = conversation.ConversationId,
                ContactId = conversation.ContactId,

                Name = conversation.Contact.Name,
                Email = conversation.Contact.Email,
                Phone = conversation.Contact.Phone,
                AvatarUrl = conversation.Contact.AvatarUrl,
                CustomAttributesJson =
                    conversation.Contact.CustomAttributesJson,
                CreatedAt = conversation.Contact.CreatedAt
            };

            // Labels của conversation hiện tại
            result.Labels = await _dbContext.ChatConversationLabels
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId)
                .OrderBy(x => x.Label.Name)
                .Select(x => new ChatContactLabelDto
                {
                    Id = x.Label.Id,
                    Name = x.Label.Name,
                    Color = x.Label.Color
                })
                .ToListAsync(cancellationToken);

            // Orders của AppUser liên kết với ChatContact
            if (!string.IsNullOrEmpty(conversation.Contact.UserId))
            {
                result.RecentOrders = await _dbContext.Orders
                    .AsNoTracking()
                    .Where(x => x.UserId == conversation.Contact.UserId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .Select(x => new ChatRecentOrderDto
                    {
                        Id = x.Id,
                        OrderCode = x.OrderCode,
                        Status = x.Status,
                        PaymentStatus = x.PaymentStatus,
                        TotalAmount = x.TotalAmount,
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync(cancellationToken);
            }

            return result;
        }
        public async Task<object> GetAdminMessagesAsync(
    long conversationId,
    int limit,
    long? before,
    CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            var query = _dbContext.ChatMessages
                .AsNoTracking()
                .Where(x =>
                    x.ConversationId == conversationId);

            if (before.HasValue)
            {
                query = query.Where(x => x.Id < before.Value);
            }

            var messages = await query
                .OrderByDescending(x => x.Id)
                .Take(limit + 1)
                .Select(x => new ChatMessageDto
                {
                    Id = x.Id,
                    ConversationId = x.ConversationId,
                    InboxId = x.InboxId,

                    ContactId = x.ContactId,
                    SenderId = x.UserId,

                    SenderName =
                        x.SenderType == ChatSenderType.Contact
                            ? x.Contact!.Name
                            : x.User!.UserName,

                    SenderAvatar =
                        x.SenderType == ChatSenderType.Contact
                            ? x.Contact!.AvatarUrl
                            : x.User!.Avatar,

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

            messages.Reverse();

            return new
            {
                Items = messages,
                HasMore = hasMore,
                OldestMessageId = messages.Count > 0
                    ? messages[0].Id
                    : (long?)null,
                NewestMessageId = messages.Count > 0
                    ? messages[^1].Id
                    : (long?)null
            };
        }

        public async Task<object> GetCustomerMessagesAsync(
    long conversationId,
    int limit,
    long? before,
    CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ChatMessages
                .AsNoTracking()
                .Where(x =>
                    x.ConversationId == conversationId &&
                    !x.IsPrivate);

            // Load các message cũ hơn message hiện tại
            if (before.HasValue)
            {
                query = query.Where(x => x.Id < before.Value);
            }

            // Lấy thêm 1 message để xác định còn dữ liệu hay không
            var messages = await query
                .OrderByDescending(x => x.Id)
                .Take(limit + 1)
                .Select(x => new ChatMessageDto
                {
                    Id = x.Id,
                    ConversationId = x.ConversationId,
                    InboxId = x.InboxId,
                    ContactId = x.ContactId,
                    SenderId = x.UserId,

                    SenderName =
                        x.SenderType == ChatSenderType.Contact
                            ? x.Contact!.Name
                            : x.User!.UserName,

                    SenderAvatar =
                        x.SenderType == ChatSenderType.Contact
                            ? x.Contact!.AvatarUrl
                            : x.User!.Avatar,

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


        #region Update Status 

        public async Task<ServiceResult> UpdateStatusAsync(
    long conversationId,
    ChatConversationStatus status,
    CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(
                    x => x.Id == conversationId,
                    cancellationToken);

            if (conversation == null)
            {
                Console.WriteLine(
                    $"[Chat] Conversation {conversationId} NOT FOUND.");

                return ServiceResult.Fail(
                    "Conversation không tồn tại.");
            }

            Console.WriteLine(
                $"[Chat] Conversation {conversationId}: " +
                $"{conversation.Status} -> {status}");

            if (conversation.Status == status)
                return ServiceResult.Success();

            // Resolved / Closed không được mở lại
            if (conversation.Status == ChatConversationStatus.Resolved ||
                conversation.Status == ChatConversationStatus.Closed)
            {
                return ServiceResult.Fail(
                    "Conversation đã kết thúc và không thể mở lại.");
            }

            conversation.Status = status;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            Console.WriteLine(
                $"[Chat] Conversation {conversationId} saved: " +
                $"{conversation.Status}");

            await _chatNotifier.NotifyConversationStatusUpdatedAsync(
                conversation.Id,
                conversation.InboxId,
                conversation.Status,
                conversation.UpdatedAt);

            return ServiceResult.Success();
        }
        public async Task<int> GetUnreadCountAsync(long conversationId, long? contactId,
            string? guestToken, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .Include(x=> x.Contact)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == conversationId &&
                        (
                            x.ContactId == contactId ||
                            _dbContext.ChatGuestSessions.Any(gs =>
                                gs.ContactId == x.ContactId &&
                                gs.TokenHash == guestToken &&
                                !gs.IsRevoked &&
                                gs.ExpiresAt > DateTime.UtcNow
                            )
                        ),
                    cancellationToken);

            if (conversation == null)
                return 0;

            return await _dbContext.ChatMessages
                .CountAsync(
                    x =>
                        x.ConversationId == conversationId &&
                        x.SenderType == ChatSenderType.Admin &&
                        x.Status != ChatMessageStatus.Read,
                    cancellationToken);
        }

        public async Task<ServiceResult> MarkConversationAsReadAsync(
    long conversationId,
    long? contactId,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            //var userId = Context.User?.FindFirstValue(
            //ClaimTypes.NameIdentifier);

            //if (string.IsNullOrWhiteSpace(userId))
            //{
            //    throw new HubException("Unauthenticated.");
            //}
            var conversation =
                await _dbContext.ChatConversations
                    .Include(x => x.Contact)
                    .FirstOrDefaultAsync(
                        x => x.Id == conversationId &&
                            (
                                x.ContactId == contactId ||
                               _dbContext.ChatGuestSessions.Any(gs =>
                                    gs.ContactId == x.ContactId &&
                                    gs.TokenHash == guestToken &&
                                    !gs.IsRevoked &&
                                    gs.ExpiresAt > DateTime.UtcNow
                                )
                            ),
                        cancellationToken);

            if (conversation == null)
            {
                return ServiceResult.Fail("Conversation not found.");
            }

            var messages =
                await _dbContext.ChatMessages
                    .Where(x =>
                        x.ConversationId == conversationId &&
                        x.SenderType == ChatSenderType.Admin &&
                        x.Status != ChatMessageStatus.Read)
                    .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                return ServiceResult.Success();
            }

            var now = DateTime.UtcNow;

            foreach (var message in messages)
            {
                message.Status = ChatMessageStatus.Read;
                message.ReadAt = now;
                message.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients
                .Group(ChatHubGroups.Conversation(conversationId))
                .SendAsync(
                    ChatHubEvents.MessageStatusUpdated,
                    new
                    {
                        ConversationId = conversationId,
                        MessageIds = messages.Select(x => x.Id).ToList(),
                        Status = ChatMessageStatus.Read
                    },
                    cancellationToken);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> MarkAllConversationsAsReadAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail(
                    "Bạn chưa đăng nhập.");
            }

            // =========================================================
            // 1. LẤY CUSTOMER MESSAGES CHƯA READ
            //
            // Hiện tại tất cả admin đều có quyền xem tất cả conversation.
            // Nếu sau này có permission theo Inbox/Team,
            // thêm filter quyền tại đây.
            // =========================================================

            var messages =
                await _dbContext.ChatMessages
                    .Where(x =>
                        x.SenderType == ChatSenderType.Contact &&
                        x.Status != ChatMessageStatus.Read)
                    .ToListAsync(
                        cancellationToken);

            if (messages.Count == 0)
            {
                return ServiceResult.Success();
            }

            var now = DateTime.UtcNow;

            // =========================================================
            // 2. UPDATE DB
            // =========================================================

            foreach (var message in messages)
            {
                message.Status =
                    ChatMessageStatus.Read;

                message.ReadAt =
                    now;

                message.UpdatedAt =
                    now;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // 3. GROUP BY CONVERSATION
            // =========================================================

            var messagesByConversation =
                messages
                    .GroupBy(x => x.ConversationId)
                    .ToList();

            // =========================================================
            // 4. REALTIME
            // =========================================================

            foreach (var group in messagesByConversation)
            {
                await _hubContext.Clients
                    .Group(
                        ChatHubGroups.Conversation(
                            group.Key))
                    .SendAsync(
                        ChatHubEvents.MessageStatusUpdated,
                        new
                        {
                            ConversationId =
                                group.Key,

                            MessageIds =
                                group.Select(x => x.Id)
                                    .ToList(),

                            Status =
                                ChatMessageStatus.Read
                        },
                        cancellationToken);
            }

            return ServiceResult.Success();
        }
        #endregion Update Status

        #region Resolved Conversation

        public async Task<ServiceResult> ResolveConversationAsync(
   long conversationId,
   string userId,
   CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(
                    x => x.Id == conversationId,
                    cancellationToken);

            if (conversation == null)
            {
                return ServiceResult.Fail("Conversation không tồn tại.");
            }

            if (conversation.Status == ChatConversationStatus.Resolved)
            {
                return ServiceResult.Success();
            }
            var now = DateTime.UtcNow;
            // 1. Tạo system message TRƯỚC khi resolve
            var systemMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                InboxId = conversation.InboxId,
                ContactId = conversation.ContactId,

                SenderType = ChatSenderType.System,
                Content = "Cuộc hội thoại kết thúc",
                Status = ChatMessageStatus.Sent,
                CreatedAt = now
            };

            _dbContext.ChatMessages.Add(systemMessage);

            // 2. Resolve conversation SAU khi đã tạo message
            conversation.Status = ChatConversationStatus.Resolved;
            conversation.ClosedAt = now;
            conversation.LastMessageAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 3. Sau SaveChanges mới broadcast message
            await _hubContext.Clients
    .Group(
        ChatHubGroups.Conversation(
            conversation.Id))
    .SendAsync(
        ChatHubEvents.MessageReceived,
        new ChatMessageDto
        {
            Id = systemMessage.Id,
            ConversationId = systemMessage.ConversationId,
            InboxId = systemMessage.InboxId,
            SenderType = systemMessage.SenderType,
            Content = systemMessage.Content,
            Status = systemMessage.Status,
            CreatedAt = systemMessage.CreatedAt
        },
        cancellationToken);

            await _hubContext.Clients
                .Group(
                    ChatHubGroups.Conversation(
                        conversation.Id))
                .SendAsync(
                    ChatHubEvents.ConversationStatusUpdated,
                    new
                    {
                        ConversationId = conversation.Id,
                        Status = ChatConversationStatus.Resolved
                    },
                    cancellationToken);

            return ServiceResult.Success();
        }

        public async Task<ChatConversationDto?> GetPreviousResolvedConversationAsync(
    long conversationId,
    long contactId,
    string? userId,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            var currentConversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == conversationId,
                    cancellationToken);

            if (currentConversation == null)
            {
                return null;
            }

            // Kiểm tra customer/guest có quyền truy cập conversation hiện tại.
            var allowed = await CanCustomerAccessConversationAsync(
                conversationId,
                contactId,
                userId,
                guestToken);

            if (!allowed)
            {
                return null;
            }

            var previousConversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x =>
                    x.InboxId == currentConversation.InboxId &&
                    x.ContactId == currentConversation.ContactId &&
                    x.Status == ChatConversationStatus.Resolved &&
                    (
                        x.CreatedAt < currentConversation.CreatedAt ||
                        (
                            x.CreatedAt == currentConversation.CreatedAt &&
                            x.Id < currentConversation.Id
                        )
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousConversation == null)
            {
                return null;
            }

            return new ChatConversationDto
            {
                Id = previousConversation.Id,
                InboxId = previousConversation.InboxId,
                ContactId = previousConversation.ContactId,
                AssignedUserId = previousConversation.AssignedUserId,
                Status = previousConversation.Status,
                Priority = previousConversation.Priority,
                Subject = previousConversation.Subject,
                LastMessageAt = previousConversation.LastMessageAt,
                ClosedAt = previousConversation.ClosedAt
            };
        }

        #endregion Resolved Conversation
        public async Task<ChatConversationStatus?> GetCustomerConversationStatusAsync(
    long conversationId,
    CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => (ChatConversationStatus?)x.Status)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ChatLabelDto> CreateLabelAsync(
     CreateChatLabelRequest request,
     CancellationToken cancellationToken = default)
        {
            var name = request.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException(
                    "Tên label không được để trống.");
            }

            var exists = await _dbContext.ChatLabels
                .AnyAsync(
                    x => x.Name == name,
                    cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    $"Label '{name}' đã tồn tại.");
            }

            var label = new ChatLabel
            {
                Name = name,
                Color = string.IsNullOrWhiteSpace(request.Color)
                    ? "#7367F0"
                    : request.Color.Trim(),
                IsActive = request.IsActive
            };

            _dbContext.ChatLabels.Add(label);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ChatLabelDto
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color,
                IsActive = label.IsActive
            };
        }

        public async Task<List<ChatLabelDto>> GetLabelsAsync(
    CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatLabels
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new ChatLabelDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteLabelAsync(
    int labelId,
    CancellationToken cancellationToken = default)
        {
            var label =
                await _dbContext.ChatLabels
                    .FirstOrDefaultAsync(
                        x => x.Id == labelId,
                        cancellationToken);

            if (label == null)
            {
                return false;
            }

            label.IsActive = false;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        public async Task<ChatContactLabelDto?> AssignConversationLabelAsync(
    long conversationId,
    int labelId,
    CancellationToken cancellationToken = default)
        {
            var conversationExists = await _dbContext.ChatConversations
                .AnyAsync(
                    x => x.Id == conversationId,
                    cancellationToken);

            if (!conversationExists)
            {
                return null;
            }

            var label = await _dbContext.ChatLabels
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == labelId && x.IsActive,
                    cancellationToken);

            if (label == null)
            {
                throw new InvalidOperationException(
                    "Label không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var exists = await _dbContext.ChatConversationLabels
                .AnyAsync(
                    x => x.ConversationId == conversationId &&
                         x.LabelId == labelId,
                    cancellationToken);

            if (exists)
            {
                return new ChatContactLabelDto
                {
                    Id = label.Id,
                    Name = label.Name,
                    Color = label.Color
                };
            }

            var conversationLabel = new ChatConversationLabel
            {
                ConversationId = conversationId,
                LabelId = labelId
            };

            _dbContext.ChatConversationLabels.Add(conversationLabel);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ChatContactLabelDto
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color
            };
        }

        public async Task<bool> RemoveConversationLabelAsync(
    long conversationId,
    int labelId,
    CancellationToken cancellationToken = default)
        {
            var conversationLabel =
                await _dbContext.ChatConversationLabels
                    .FirstOrDefaultAsync(
                        x => x.ConversationId == conversationId &&
                             x.LabelId == labelId,
                        cancellationToken);

            if (conversationLabel == null)
            {
                return false;
            }

            _dbContext.ChatConversationLabels.Remove(conversationLabel);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<ChatContact> ResolveAuthenticatedContactAsync(
    string userId,
    CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException(
                    "Không xác định được người dùng.");
            }

            userId = userId.Trim();

            // =========================================================
            // 1. Tìm canonical ChatContact của authenticated customer
            // =========================================================

            var contact =
                await _dbContext.ChatContacts
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId,
                        ct);

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

            var hasContactId =
                contactId.HasValue &&
                contactId.Value > 0;

            var hasGuestToken =
                !string.IsNullOrWhiteSpace(guestToken);

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

                var token =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(32));

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

                var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

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

        private async Task<(ChatConversation Conversation, bool IsNewConversation)>
    ResolveConversationForCustomerAsync(
        ChatContact contact,
        ChatInbox inbox,
        long? conversationId,
        long inboxId,
        CancellationToken ct)
        {
            // =========================================================
            // 1. Nếu client có conversationId
            // =========================================================

            if (conversationId.HasValue)
            {
                var existing =
                    await _dbContext.ChatConversations
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id == conversationId.Value &&
                                x.ContactId == contact.Id &&
                                x.InboxId == inboxId,
                            ct);

                // Conversation không tồn tại hoặc không thuộc Contact/Inbox
                if (existing == null)
                {
                    throw new HubException("CHAT_SESSION_INVALID");
                }

                // =====================================================
                // 2. Conversation đang Active
                // => tiếp tục conversation này
                // =====================================================

                if (existing.Status == ChatConversationStatus.Open ||
                    existing.Status == ChatConversationStatus.Pending)
                {
                    return (existing, false);
                }

                // =====================================================
                // 3. Conversation đã Resolved / Closed
                // => KHÔNG reopen
                // => tìm conversation Active khác của Contact
                // =====================================================

                var activeConversation =
                    await _dbContext.ChatConversations
                        .Where(x =>
                            x.ContactId == contact.Id &&
                            x.InboxId == inboxId &&
                            (
                                x.Status == ChatConversationStatus.Open ||
                                x.Status == ChatConversationStatus.Pending
                            ))
                        .OrderByDescending(x => x.LastMessageAt)
                        .FirstOrDefaultAsync(ct);

                if (activeConversation != null)
                {
                    return (activeConversation, false);
                }

                // Không còn conversation Active
                // => tạo conversation mới
                return CreateNewConversation(
                    contact,
                    inbox,
                    existing.Priority);
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

            var currentConversation =
                await _dbContext.ChatConversations
                    .Where(x =>
                        x.ContactId == contact.Id &&
                        x.InboxId == inboxId &&
                        (
                            x.Status == ChatConversationStatus.Open ||
                            x.Status == ChatConversationStatus.Pending
                        ))
                    .OrderByDescending(x => x.LastMessageAt)
                    .FirstOrDefaultAsync(ct);

            if (currentConversation != null)
            {
                return (currentConversation, false);
            }

            // =========================================================
            // 5. Không có conversation Active
            // => tạo conversation mới
            // =========================================================

            return CreateNewConversation(
                contact,
                inbox,
                ChatConversationPriority.Low);
        }

        private (ChatConversation Conversation, bool IsNewConversation)
    CreateNewConversation(
        ChatContact contact,
        ChatInbox inbox,
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

        private async Task<ChatContact> ClaimOrMergeGuestContactAsync(
    long guestContactId,
    string guestToken,
    string userId,
    CancellationToken ct)
        {
            if (guestContactId <= 0)
                throw new HubException("CHAT_SESSION_INVALID");

            if (string.IsNullOrWhiteSpace(guestToken))
                throw new HubException("CHAT_SESSION_INVALID");

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("CHAT_SESSION_INVALID");

            userId = userId.Trim();

            var now = DateTime.UtcNow;

            // =========================================================
            // 1. Lấy guest contact
            // =========================================================

            var guestContact =
                await _dbContext.ChatContacts
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == guestContactId &&
                            !x.IsMerged,
                        ct);

            if (guestContact == null)
                throw new HubException("CHAT_SESSION_INVALID");

            // =========================================================
            // 2. Validate guest token
            // =========================================================

            var tokenHash = Common.CommonHelper.Hash(guestToken);

            var guestSession =
                await _dbContext.ChatGuestSessions
                    .FirstOrDefaultAsync(
                        x =>
                            x.ContactId == guestContact.Id &&
                            x.TokenHash == tokenHash &&
                            !x.IsRevoked,
                        ct);

            if (guestSession == null)
                throw new HubException("CHAT_SESSION_INVALID");

            // Login claim/merge KHÔNG rotate token.
            // Token hết hạn => session không còn hợp lệ.
            if (guestSession.ExpiresAt <= now)
                throw new HubException("CHAT_SESSION_INVALID");

            // =========================================================
            // 3. Tìm canonical contact của user
            // =========================================================

            var canonicalContact =
                await _dbContext.ChatContacts
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId == userId &&
                            !x.IsMerged,
                        ct);

            // =========================================================
            // 4. User chưa có Contact
            //    => claim guest contact
            // =========================================================

            if (canonicalContact == null)
            {
                guestContact.UserId = userId;
                guestContact.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(ct);

                return guestContact;
            }

            // =========================================================
            // 5. Guest contact chính là canonical contact
            // =========================================================

            if (canonicalContact.Id == guestContact.Id)
            {
                return canonicalContact;
            }

            // =========================================================
            // 6. User đã có canonical Contact
            //    => merge guest -> canonical
            // =========================================================

            await MergeGuestContactIntoCanonicalAsync(
                guestContact,
                canonicalContact,
                now,
                ct);

            return canonicalContact;
        }

        private async Task MergeGuestContactIntoCanonicalAsync(
    ChatContact guestContact,
    ChatContact canonicalContact,
    DateTime now,
    CancellationToken ct)
        {
            if (guestContact.Id == canonicalContact.Id)
                return;

            // =========================================================
            // 1. Migrate Conversations
            // =========================================================

            var conversations =
                await _dbContext.ChatConversations
                    .Where(x =>
                        x.ContactId == guestContact.Id)
                    .ToListAsync(ct);

            foreach (var conversation in conversations)
            {
                conversation.ContactId = canonicalContact.Id;
                conversation.UpdatedAt = now;
            }

            // =========================================================
            // 2. Migrate Messages
            // =========================================================

            var messages =
                await _dbContext.ChatMessages
                    .Where(x =>
                        x.ContactId == guestContact.Id)
                    .ToListAsync(ct);

            foreach (var message in messages)
            {
                message.ContactId = canonicalContact.Id;
                message.UpdatedAt = now;
            }

            // =========================================================
            // 3. Migrate ChatContactInbox
            //
            // Không được update trực tiếp tất cả ContactId
            // vì có thể tồn tại:
            //
            // Guest C1 + Inbox 1
            // Canonical C0 + Inbox 1
            //
            // => duplicate unique key sau khi đổi C1 -> C0
            // =========================================================

            var guestContactInboxes =
                await _dbContext.ChatContactInboxes
                    .Where(x =>
                        x.ContactId == guestContact.Id)
                    .ToListAsync(ct);

            var canonicalInboxIds =
                await _dbContext.ChatContactInboxes
                    .Where(x =>
                        x.ContactId == canonicalContact.Id)
                    .Select(x => x.InboxId)
                    .ToListAsync(ct);

            var canonicalInboxIdSet =
                canonicalInboxIds.ToHashSet();

            foreach (var guestContactInbox in guestContactInboxes)
            {
                if (canonicalInboxIdSet.Contains(
                        guestContactInbox.InboxId))
                {
                    // Canonical đã có mapping này.
                    // Không migrate record này để tránh duplicate.
                    _dbContext.ChatContactInboxes.Remove(
                        guestContactInbox);

                    continue;
                }

                guestContactInbox.ContactId =
                    canonicalContact.Id;
            }

            // =========================================================
            // 4. Migrate Guest Sessions
            // =========================================================

            var guestSessions =
                await _dbContext.ChatGuestSessions
                    .Where(x =>
                        x.ContactId == guestContact.Id)
                    .ToListAsync(ct);

            foreach (var session in guestSessions)
            {
                session.ContactId =
                    canonicalContact.Id;
            }

            // =========================================================
            // 5. Mark guest contact as merged
            // =========================================================

            guestContact.IsMerged = true;
            guestContact.MergedIntoContactId =
                canonicalContact.Id;
            guestContact.MergedAt = now;
            guestContact.UpdatedAt = now;

            // Không xóa guestContact.
            //
            // Giữ lại record để:
            // - bảo toàn lịch sử
            // - có thể audit
            // - tránh FK/history problem
            //
            // Sau này nếu muốn hard delete thì xử lý riêng.
            // =========================================================

            await _dbContext.SaveChangesAsync(ct);
        }

        private async Task ReconcileActiveConversationsAfterGuestLoginAsync(
    long contactId,
    long inboxId,
    long currentConversationId,
    string userId,
    CancellationToken ct)
        {
            var activeConversationIds =
                await _dbContext.ChatConversations
                    .Where(x =>
                        x.ContactId == contactId &&
                        x.InboxId == inboxId &&
                        (
                            x.Status == ChatConversationStatus.Open ||
                            x.Status == ChatConversationStatus.Pending
                        ) &&
                        x.Id != currentConversationId)
                    .Select(x => x.Id)
                    .ToListAsync(ct);

            foreach (var conversationId in activeConversationIds)
            {
                var result =
                    await ResolveConversationAsync(
                        conversationId,
                        userId,
                        ct);

                if (!result.Succeeded)
                {
                    throw new HubException(result.Message);
                }
            }
        }
    }
}
