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
using System.Reflection.Emit;

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

            var now = DateTime.UtcNow;

            var isAuthenticated =
                !string.IsNullOrWhiteSpace(userId);

            var hasGuestToken =
                !string.IsNullOrWhiteSpace(
                    request.GuestToken);

            string? guestTokenHash = null;

            if (hasGuestToken)
            {
                guestTokenHash =
                    Common.CommonHelper.Hash(
                        request.GuestToken!);
            }

            ChatContact? contact = null;

            // =========================================================
            // 1. CUSTOMER
            // =========================================================

            if (isAuthenticated)
            {
                // -----------------------------------------------------
                // 1A. Tìm Contact theo UserId
                // -----------------------------------------------------

                contact =
                    await _dbContext.ChatContacts
                        .FirstOrDefaultAsync(
                            x => x.UserId == userId,
                            cancellationToken);

                // -----------------------------------------------------
                // 1B. Customer chưa có Contact
                //     → thử claim Guest Contact
                // -----------------------------------------------------

                if (
                    contact == null &&
                    guestTokenHash != null)
                {
                    var guestSession =
                        await _dbContext.ChatGuestSessions
                            .FirstOrDefaultAsync(
                                x =>
                                    x.TokenHash ==
                                        guestTokenHash,
                                cancellationToken);

                    if (
                        guestSession != null &&
                        !guestSession.IsRevoked &&
                        guestSession.ExpiresAt > now)
                    {
                        var guestContact =
                            await _dbContext.ChatContacts
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.Id ==
                                        guestSession.ContactId,
                                    cancellationToken);

                        if (guestContact != null)
                        {
                            if (
                                string.IsNullOrWhiteSpace(
                                    guestContact.UserId))
                            {
                                guestContact.UserId =
                                    userId;

                                guestContact.UpdatedAt =
                                    now;

                                guestSession.LastSeenAt =
                                    now;

                                contact =
                                    guestContact;
                            }
                            else if (
                                guestContact.UserId ==
                                userId)
                            {
                                guestSession.LastSeenAt =
                                    now;

                                contact =
                                    guestContact;
                            }
                        }
                    }
                }

                // -----------------------------------------------------
                // 1C. Customer hoàn toàn mới
                // -----------------------------------------------------

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

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(
                            request.Name))
                    {
                        contact.Name =
                            request.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            request.Email))
                    {
                        contact.Email =
                            request.Email;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            request.Phone))
                    {
                        contact.Phone =
                            request.Phone;
                    }

                    contact.UpdatedAt = now;
                }
            }

            // =========================================================
            // 2. GUEST
            // =========================================================

            else
            {
                if (!hasGuestToken)
                {
                    throw new InvalidOperationException(
                        "GuestToken là bắt buộc đối với guest.");
                }

                var guestSession =
                    await _dbContext.ChatGuestSessions
                        .FirstOrDefaultAsync(
                            x =>
                                x.TokenHash ==
                                guestTokenHash,
                            cancellationToken);

                // -----------------------------------------------------
                // 2A. Guest session tồn tại
                // -----------------------------------------------------

                if (guestSession != null)
                {
                    if (
                        guestSession.IsRevoked ||
                        guestSession.ExpiresAt <= now)
                    {
                        throw new InvalidOperationException(
                            "Guest session đã hết hạn hoặc không còn hợp lệ.");
                    }

                    contact =
                        await _dbContext.ChatContacts
                            .FirstOrDefaultAsync(
                                x =>
                                    x.Id ==
                                    guestSession.ContactId,
                                cancellationToken);

                    if (contact == null)
                    {
                        throw new InvalidOperationException(
                            "Guest contact không tồn tại.");
                    }

                    guestSession.LastSeenAt =
                        now;

                    if (!string.IsNullOrWhiteSpace(
                            request.Name))
                    {
                        contact.Name =
                            request.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            request.Email))
                    {
                        contact.Email =
                            request.Email;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            request.Phone))
                    {
                        contact.Phone =
                            request.Phone;
                    }

                    contact.UpdatedAt =
                        now;
                }

                // -----------------------------------------------------
                // 2B. Guest hoàn toàn mới
                // -----------------------------------------------------

                else
                {
                    contact = new ChatContact
                    {
                        UserId = null,
                        Name = request.Name,
                        Email = request.Email,
                        Phone = request.Phone,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _dbContext.ChatContacts.Add(contact);

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    var newGuestSession =
                        new ChatGuestSession
                        {
                            ContactId = contact.Id,
                            TokenHash =
                                guestTokenHash!,
                            ExpiresAt =
                                now.AddDays(30),
                            IsRevoked = false,
                            CreatedAt = now,
                            LastSeenAt = now
                        };

                    _dbContext.ChatGuestSessions.Add(
                        newGuestSession);
                }
            }

            // =========================================================
            // 3. CONTACT - INBOX
            // =========================================================

            var contactInbox =
                await _dbContext.ChatContactInboxes
                    .FirstOrDefaultAsync(
                        x =>
                            x.ContactId ==
                                contact!.Id &&
                            x.InboxId ==
                                inbox.Id,
                        cancellationToken);

            if (contactInbox == null)
            {
                contactInbox =
                    new ChatContactInbox
                    {
                        ContactId =
                            contact!.Id,

                        InboxId =
                            inbox.Id,

                        CreatedAt =
                            now
                    };

                _dbContext.ChatContactInboxes.Add(
                    contactInbox);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // 4. KHÔNG TẠO CONVERSATION
            //
            // Conversation chỉ được tạo khi customer gửi message.
            // =========================================================

            return new StartChatResponse
            {
                ContactId = contact!.Id,
                InboxId = inbox.Id,
                ConversationId = null,

                ContactName =
                    contact.Name,

                ContactAvatar =
                    contact.AvatarUrl
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
        public async Task<ChatMessageDto> SendCustomerMessageAsync(
    long? conversationId,
    long contactId,
    string content,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            content = content.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "Nội dung tin nhắn không được rỗng.");
            }

            var contact =
                await _dbContext.ChatContacts
                    .FirstOrDefaultAsync(
                        x => x.Id == contactId,
                        cancellationToken);

            if (contact == null)
            {
                throw new InvalidOperationException(
                    "Contact không tồn tại.");
            }

            var now = DateTime.UtcNow;

            ChatConversation conversation;

            // =========================================================
            // 1. KHÔNG CÓ CONVERSATION
            //
            // Customer gửi message đầu tiên.
            // → CREATE A
            // =========================================================

            if (!conversationId.HasValue)
            {
                var contactInbox =
     await _dbContext.ChatContactInboxes
         .FirstOrDefaultAsync(
             x => x.ContactId == contactId,
             cancellationToken);
                if (contactInbox == null)
                {
                    throw new InvalidOperationException(
                        "Contact chưa thuộc inbox.");
                }

                conversation =
                    new ChatConversation
                    {
                        InboxId =
                            contactInbox.InboxId,

                        ContactId =
                            contactId,

                        Status =
                            ChatConversationStatus.Pending,

                        Priority =
                            ChatConversationPriority.Low,

                        LastMessageAt =
                            now,

                        CreatedAt =
                            now,

                        UpdatedAt =
                            now
                    };

                _dbContext.ChatConversations.Add(
                    conversation);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            // =========================================================
            // 2. ĐÃ CÓ CONVERSATION
            // =========================================================

            else
            {
                var oldConversation =
                    await _dbContext.ChatConversations
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                    conversationId.Value &&
                                x.ContactId ==
                                    contactId,
                            cancellationToken);

                if (oldConversation == null)
                {
                    throw new InvalidOperationException(
                        "Conversation không tồn tại.");
                }

                // -----------------------------------------------------
                // CLOSED
                // -----------------------------------------------------

                if (
                    oldConversation.Status ==
                    ChatConversationStatus.Closed)
                {
                    throw new InvalidOperationException(
                        "Conversation đã đóng.");
                }

                // -----------------------------------------------------
                // RESOLVED
                //
                // A Resolved → tạo B
                // -----------------------------------------------------

                if (
                    oldConversation.Status ==
                    ChatConversationStatus.Resolved)
                {
                    conversation =
                        new ChatConversation
                        {
                            InboxId =
                                oldConversation.InboxId,

                            ContactId =
                                oldConversation.ContactId,

                            Status =
                                ChatConversationStatus.Pending,

                            Priority =
                                ChatConversationPriority.Low,

                            LastMessageAt =
                                now,

                            CreatedAt =
                                now,

                            UpdatedAt =
                                now
                        };

                    _dbContext.ChatConversations.Add(
                        conversation);

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }

                // -----------------------------------------------------
                // OPEN / PENDING
                //
                // Tiếp tục conversation hiện tại.
                // -----------------------------------------------------

                else
                {
                    conversation =
                        oldConversation;
                }
            }

            // =========================================================
            // 3. CREATE MESSAGE
            // =========================================================

            var message =
                new ChatMessage
                {
                    ConversationId =
                        conversation.Id,

                    InboxId =
                        conversation.InboxId,

                    ContactId =
                        contact.Id,

                    UserId =
                        contact.UserId,

                    MessageType =
                        ChatMessageType.Incoming,

                    ContentType =
                        ChatMessageContentType.Text,

                    Status =
                        ChatMessageStatus.Sent,

                    SenderType =
                        ChatSenderType.Contact,

                    Content =
                        content,

                    IsPrivate =
                        false,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now
                };

            _dbContext.ChatMessages.Add(
                message);

            conversation.UpdatedAt =
                now;

            conversation.Subject =
                message.Content;

            conversation.LastMessageAt =
                message.CreatedAt;

            // =========================================================
            // 4. STATUS
            // =========================================================

            var isAdminViewing =
                _chatPresenceService
                    .IsConversationActive(
                        conversation.Id);

            var statusChanged =
                false;

            if (
                !isAdminViewing &&
                conversation.Status !=
                    ChatConversationStatus.Pending)
            {
                conversation.Status =
                    ChatConversationStatus.Pending;

                statusChanged = true;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // 5. STATUS REALTIME
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
            // 6. DTO
            // =========================================================

            var dto =
                new ChatMessageDto
                {
                    Id =
                        message.Id,

                    ConversationId =
                        message.ConversationId,

                    InboxId =
                        message.InboxId,

                    ContactId =
                        message.ContactId,

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
            // 7. REALTIME
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
    long? conversationId,
    long contactId,
    string? userId,
    string? guestToken,
    CancellationToken cancellationToken = default)
        {
            // Chưa có Conversation:
            // Đây là lần gửi tin nhắn đầu tiên.
            // Chỉ cần xác thực Contact/identity.
            if (!conversationId.HasValue)
            {
                if (contactId <= 0)
                {
                    return false;
                }

                // Customer đã đăng nhập
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return await _dbContext.ChatContacts
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.Id == contactId &&
                                x.UserId == userId,
                            cancellationToken);
                }

                // Guest
                if (string.IsNullOrWhiteSpace(guestToken))
                {
                    return false;
                }

                var tokenHash =
                    Common.CommonHelper.Hash(guestToken);

                return await _dbContext.ChatGuestSessions
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.ContactId == contactId &&
                            x.TokenHash == tokenHash &&
                            !x.IsRevoked &&
                            x.ExpiresAt > DateTime.UtcNow,
                        cancellationToken);
            }

            // Đã có Conversation
            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId.Value)
                .Select(x => new
                {
                    x.ContactId,
                    ContactUserId = x.Contact.UserId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversation == null)
            {
                return false;
            }

            // Customer đã đăng nhập
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return conversation.ContactId == contactId
                    && conversation.ContactUserId == userId;
            }

            // Guest
            if (conversation.ContactId != contactId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(guestToken))
            {
                return false;
            }

            var tokenHashForGuest =
                Common.CommonHelper.Hash(guestToken);

            return await _dbContext.ChatGuestSessions
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ContactId == contactId &&
                        x.TokenHash == tokenHashForGuest &&
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

        public async Task<ServiceResult> MarkAllConversationsAsReadAsync(CancellationToken cancellationToken = default)
        {
            var messages =
                await _dbContext.ChatMessages
                    .Where(x =>
                        x.SenderType == ChatSenderType.Contact &&
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

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // Group theo conversation để broadcast
            var conversationIds =
                messages
                    .Select(x => x.ConversationId)
                    .Distinct()
                    .ToList();

            foreach (var conversationId in conversationIds)
            {
                var messageIds =
                    messages
                        .Where(x =>
                            x.ConversationId ==
                            conversationId)
                        .Select(x => x.Id)
                        .ToList();

                await _hubContext.Clients
                    .Group(
                        ChatHubGroups.Conversation(
                            conversationId))
                    .SendAsync(
                        ChatHubEvents.MessageStatusUpdated,
                        new
                        {
                            ConversationId =
                                conversationId,
                            MessageIds =
                                messageIds,
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
    }
}
