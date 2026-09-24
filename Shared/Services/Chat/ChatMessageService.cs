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
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Notification;
using Shared.Requests.Chat;
using Shared.Responses;
using System.Runtime.CompilerServices;
using System.Security.Claims;

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

        public async Task<ChatConversationDto> StartConversationAsync(
    StartChatRequest request,
    string? userId,
    CancellationToken cancellationToken = default)
        {
            var inbox = await _dbContext.ChatInboxes
                .FirstOrDefaultAsync(
                    x => x.Id == request.InboxId && x.IsActive,
                    cancellationToken);

            if (inbox == null)
                throw new InvalidOperationException(
                    "Chat inbox không tồn tại hoặc đã bị tắt.");

            var now = DateTime.UtcNow;

            ChatContact? contact = null;

            // =========================================================
            // 1. LOGGED-IN CUSTOMER
            // =========================================================
            if (!string.IsNullOrWhiteSpace(userId))
            {
                contact = await _dbContext.ChatContacts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            }

            // =========================================================
            // 2. GUEST
            // =========================================================
            string? guestToken = null;
            string? guestTokenHash = null;

            if (string.IsNullOrWhiteSpace(userId))
            {
                if (string.IsNullOrWhiteSpace(request.GuestToken))
                    throw new InvalidOperationException(
                        "Guest token không hợp lệ.");

                // Không nên lấy contact theo Email làm identity
                // GuestToken mới là identity của guest session.

                //var token = request.GuestToken != null ? request.GuestToken : Common.CommonHelper.GenerateSecureToken();
                guestToken = request.GuestToken;
                guestTokenHash = Common.CommonHelper.Hash(guestToken);

                var guestSession = await _dbContext.ChatGuestSessions
                    .FirstOrDefaultAsync(
                        x =>
                            x.TokenHash == guestTokenHash &&
                            !x.IsRevoked &&
                            x.ExpiresAt > now,
                        cancellationToken);

                if (guestSession != null)
                {
                    contact = await _dbContext.ChatContacts.FirstOrDefaultAsync(x => x.Id == guestSession.ContactId,
                    cancellationToken);
                    guestSession.LastSeenAt = now;
                }
            }

            // =========================================================
            // 3. FALLBACK CONTACT
            // =========================================================
            // Chỉ nên dùng email để tìm contact trong trường hợp
            // customer đăng nhập / hoặc flow identity của bạn cho phép.
            if (contact == null &&
                !string.IsNullOrWhiteSpace(userId) &&
                !string.IsNullOrWhiteSpace(request.Email))
            {
                contact = await _dbContext.ChatContacts.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
            }

            // =========================================================
            // 4. CREATE CONTACT
            // =========================================================
            if (contact == null)
            {
                contact = new ChatContact
                {
                    UserId = userId,
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.ChatContacts.Add(contact);

                // Cần Save để lấy contact.Id
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.Name))
                    contact.Name = request.Name;

                if (!string.IsNullOrWhiteSpace(request.Email))
                    contact.Email = request.Email;

                if (!string.IsNullOrWhiteSpace(request.Phone))
                    contact.Phone = request.Phone;

                if (!string.IsNullOrWhiteSpace(userId) &&
                    string.IsNullOrWhiteSpace(contact.UserId))
                {
                    contact.UserId = userId;
                }

                contact.UpdatedAt = now;
            }

            // =========================================================
            // GUEST SESSION
            // =========================================================
            if (string.IsNullOrWhiteSpace(userId))
            {
                var guestSession = await _dbContext.ChatGuestSessions
                    .FirstOrDefaultAsync(
                        x =>
                            x.ContactId == contact.Id &&
                            x.TokenHash == guestTokenHash &&
                            !x.IsRevoked &&
                            x.ExpiresAt > now,
                        cancellationToken);

                if (guestSession == null)
                {
                    guestSession = new ChatGuestSession
                    {
                        ContactId = contact.Id,
                        TokenHash = guestTokenHash!,
                        ExpiresAt = now.AddDays(30),
                        IsRevoked = false,
                        CreatedAt = now,
                        LastSeenAt = now,
                    };

                    _dbContext.ChatGuestSessions.Add(guestSession);
                }
            }

            // =========================================================
            // 6. CONTACT - INBOX
            // =========================================================
            var contactInbox = await _dbContext.ChatContactInboxes
                .FirstOrDefaultAsync(
                    x =>
                        x.ContactId == contact.Id &&
                        x.InboxId == inbox.Id,
                    cancellationToken);

            if (contactInbox == null)
            {
                contactInbox = new ChatContactInbox
                {
                    ContactId = contact.Id,
                    InboxId = inbox.Id,
                    CreatedAt = now
                };

                _dbContext.ChatContactInboxes.Add(contactInbox);
            }

            // =========================================================
            // 7. FIND OPEN CONVERSATION
            // =========================================================
            var conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(
                    x =>
                        x.InboxId == inbox.Id &&
                        x.ContactId == contact.Id &&
                        x.Status != ChatConversationStatus.Closed &&
                        x.Status != ChatConversationStatus.Resolved,
                    cancellationToken);

            // =========================================================
            // 8. CREATE CONVERSATION
            // =========================================================
            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    InboxId = inbox.Id,
                    ContactId = contact.Id,
                    Status = ChatConversationStatus.Open,
                    Priority = ChatConversationPriority.Low,
                    LastMessageAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.ChatConversations.Add(conversation);
            }
            else
            {
                conversation.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // =========================================================
            // 9. RESULT
            // =========================================================
            return new ChatConversationDto
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
        }

        public async Task<ChatMessageDto> SendCustomerMessageAsync(
    long conversationId,
    long contactId,
    string content,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            content = content.Trim();

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException(
                    "Nội dung tin nhắn không được rỗng.");

            var conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == conversationId &&
                        x.ContactId == contactId,
                    cancellationToken);

            if (conversation == null)
                throw new InvalidOperationException(
                    "Conversation không tồn tại.");

            if (conversation.Status == ChatConversationStatus.Closed)
                throw new InvalidOperationException(
                    "Conversation đã đóng.");

            var contact = await _dbContext.ChatContacts
                .FirstOrDefaultAsync(
                    x => x.Id == contactId,
                    cancellationToken);

            if (contact == null)
                throw new InvalidOperationException(
                    "Contact không tồn tại.");

            var now = DateTime.UtcNow;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                InboxId = conversation.InboxId,
                ContactId = contact.Id,

                // Guest sẽ null, logged-in customer sẽ có UserId
                UserId = contact.UserId,

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

            conversation.UpdatedAt = message.CreatedAt;
            conversation.Subject = message.Content;
            conversation.LastMessageAt = message.CreatedAt;

            var isAdminViewing = _chatPresenceService.IsConversationActive(
        conversation.Id);
            var statusChanged = false;

            if (!isAdminViewing &&
                conversation.Status != ChatConversationStatus.Pending)
            {
                conversation.Status =
                    ChatConversationStatus.Pending;
                statusChanged = true;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (statusChanged)
            {
                await _chatNotifier.NotifyConversationStatusUpdatedAsync(
                    conversation.Id,
                    conversation.InboxId,
                    conversation.Status,
                    conversation.UpdatedAt);
            }
            var dto = new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                InboxId = message.InboxId,
                ContactId = message.ContactId,
                SenderId = message.UserId,

                SenderName = contact.Name,
                SenderAvatar = contact.AvatarUrl,

                MessageType = message.MessageType,
                ContentType = message.ContentType,
                Status = message.Status,
                SenderType = message.SenderType,

                Content = message.Content,
                IsPrivate = message.IsPrivate,

                CreatedAt = message.CreatedAt
            };

            await _hubContext.Clients
                .Group(ChatHubGroups.Conversation(conversation.Id))
                .SendAsync(
                    ChatHubEvents.MessageReceived,
                    dto,
                    cancellationToken);

            await _hubContext.Clients
                .Group(ChatHubGroups.Inbox(conversation.InboxId))
                .SendAsync(
                    ChatHubEvents.MessageReceived,
                    dto,
                    cancellationToken);

            return dto;
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

            if (conversation.Status == ChatConversationStatus.Closed)
                throw new InvalidOperationException(
                    "Conversation đã đóng.");
            var now = DateTime.UtcNow;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,

                InboxId = conversation.InboxId,

                ContactId = null,

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
        public async Task<object> GetConversationListAsync(
    long? inboxId,
    string? currentUserId,
    int limit = 30,
    DateTime? beforeLastMessageAt = null,
    long? beforeId = null,
    CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            var query = _dbContext.ChatConversations
                .AsNoTracking().AsQueryable();
            if (inboxId.HasValue)
            {
                query = query.Where(x =>
                    x.InboxId == inboxId.Value);
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

            var conversations = await query
                .OrderByDescending(x => x.LastMessageAt)
                .ThenByDescending(x => x.Id)
                .Take(limit + 1)
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

                OldestLastMessageAt = conversations.Count > 0
        ? conversations[^1].LastMessageAt
        : null,

                OldestId = conversations.Count > 0
        ? conversations[^1].Id
        : null
            };
        }

        public async Task<ChatConversationCountsDto> GetConversationCountsAsync(
     long? inboxId,
     string? search = null,
     string? assignedUserId = null,
     long? labelId = null,
     CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ChatConversations
                .AsNoTracking()
        .AsQueryable();
            if (inboxId.HasValue)
            {
                query = query.Where(x =>
                    x.InboxId == inboxId.Value);
            }
            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Contact.Name.Contains(search) ||
                    x.Contact.Email.Contains(search) ||
                    x.Contact.Phone.Contains(search) ||
                    (x.Subject != null && x.Subject.Contains(search)));
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
    long contactId,
    string? userId,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new
                {
                    x.ContactId,
                    ContactUserId = x.Contact.UserId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversation == null ||
                conversation.ContactId != contactId)
            {
                return false;
            }

            // Logged-in customer
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return conversation.ContactUserId == userId;
            }

            // Guest
            if (string.IsNullOrWhiteSpace(guestToken))
                return false;

            var tokenHash = Common.CommonHelper.Hash(guestToken);

            return await _dbContext.ChatGuestSessions
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ContactId == contactId &&
                    x.TokenHash == tokenHash &&
                    !x.IsRevoked &&
                    x.ExpiresAt > DateTime.UtcNow,
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
                return null;

            var result = new ChatConversationContactDto
            {
                ConversationId = conversation.ConversationId,
                ContactId = conversation.Contact.Id,

                Name = conversation.Contact.Name,
                Email = conversation.Contact.Email,
                Phone = conversation.Contact.Phone,
                AvatarUrl = conversation.Contact.AvatarUrl,
                CustomAttributesJson = conversation.Contact.CustomAttributesJson,
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

            conversation.Status = status;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            Console.WriteLine(
        $"[Chat] Conversation {conversationId} saved: {conversation.Status}");
            await _chatNotifier.NotifyConversationStatusUpdatedAsync(
                conversation.Id, conversation.InboxId,
                conversation.Status,
                conversation.UpdatedAt);

            return ServiceResult.Success();
        }
        public async Task<int> GetUnreadCountAsync(long conversationId, long? contactId,
            string? guestToken, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == conversationId &&
                        (
                            x.ContactId == contactId ||
                            x.Contact.GuestToken == guestToken
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
            var conversation =
                await _dbContext.ChatConversations
                    .Include(x => x.Contact)
                    .FirstOrDefaultAsync(
                        x => x.Id == conversationId &&
                            (
                                x.ContactId == contactId ||
                                x.Contact.GuestToken == guestToken
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
        #endregion Update Status
    }
}
