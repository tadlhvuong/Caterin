using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Core;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.DTOs.Chat;
using Shared.Enums;
using Shared.Enums.Chat;
using Shared.Interfaces.Chat;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Shared.Extensions
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _dbContext;
        private readonly IChatMessageService _chatMessageService;
        private readonly IChatPresenceService _chatPresenceService;

        public ChatHub(AppDbContext dbContext, IChatMessageService chatMessageService, IChatPresenceService chatPresenceService)
        {
            _dbContext = dbContext;
            _chatMessageService = chatMessageService;
            _chatPresenceService = chatPresenceService;
        }
        public override async Task OnConnectedAsync()
        {
            var userId =
                Context.User?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var clientType =
                Context.GetHttpContext()?
                    .Request.Query["clientType"]
                    .ToString();

            if (
                clientType == "admin" &&
                !string.IsNullOrWhiteSpace(userId))
            {
                var becameOnline =
                    await _chatPresenceService.AddConnectionAsync(
                        userId,
                        Context.ConnectionId);

                if (becameOnline)
                {
                    await Clients.All.SendAsync(
                        "chat.admin.online",
                        new
                        {
                            userId
                        });
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(
    Exception? exception)
        {
            var userId =
                Context.User?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var clientType =
                Context.GetHttpContext()?
                    .Request.Query["clientType"]
                    .ToString();

            if (clientType == "admin" && !string.IsNullOrWhiteSpace(userId))
            {
                var becameOffline = await _chatPresenceService.RemoveConnectionAsync(userId, Context.ConnectionId);

                if (becameOffline)
                {
                    await Clients.All.SendAsync(
                        "chat.admin.offline",
                        new
                        {
                            userId
                        });
                }
            }

            await _chatPresenceService.RemoveConnectionFromConversationsAsync(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        [Authorize]
        public async Task<List<string>> GetOnlineAdmins()
        {
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Unauthenticated.");

            return _chatPresenceService
                .GetOnlineAdminIds()
                .ToList();
        }
        public Task<bool> IsAnyAdminOnline()
        {
            return Task.FromResult(_chatPresenceService.IsAnyOnline());
        }
        public async Task<ChatConversationStatus> JoinConversation(
    long conversationId,
    long contactId,
    string? guestToken)
        {
            var userId = Context.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            var allowed =
                await _chatMessageService
                    .CanCustomerAccessConversationAsync(
                        conversationId,
                        contactId,
                        userId,
                        guestToken,
                        Context.ConnectionAborted);

            if (!allowed)
            {
                throw new HubException(
                    "Bạn không có quyền truy cập conversation này.");
            }

            var status =
                await _chatMessageService
                    .GetCustomerConversationStatusAsync(
                        conversationId,
                        Context.ConnectionAborted);

            if (!status.HasValue)
            {
                throw new HubException(
                    "Conversation không tồn tại.");
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Conversation(conversationId),
                Context.ConnectionAborted);

            return status.Value;
        }
        [Authorize]
        public async Task JoinAdminConversation(
    long conversationId)
        {
            var userId = Context.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Unauthenticated.");

            var allowed =
                await _chatMessageService.CanAccessConversationAsync(
                    conversationId,
                    null,
                    userId,
                    isAdmin: true);

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền truy cập conversation này.");

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Conversation(conversationId));

            await _chatPresenceService.JoinConversationAsync(
       userId,
       Context.ConnectionId,
       conversationId);

            await Clients.Group(ChatHubGroups.Conversation(conversationId)).SendAsync(
            ChatHubEvents.AdminConvensationPresenceUpdated,
            new
            {
                userId,
                conversationId,
                isOnline = true
            });
        }
        [Authorize]
        [PermissionAction(ActionType.View)]
        public async Task JoinInbox(long inboxId)
        {
            var userId = Context.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Unauthenticated.");

            // Phase 1:
            // kiểm tra quyền admin/inbox ở đây.

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Inbox(inboxId));
        }
        public async Task<ChatMessageDto> SendCustomerMessage(
    long? conversationId,
    long contactId,
    string content,
    string? guestToken)
        {
            var userId =
                Context.User?
                    .FindFirst(
                        ClaimTypes.NameIdentifier)?
                    .Value;

            var allowed =
                await _chatMessageService
                    .CanCustomerAccessConversationAsync(
                        conversationId,
                        contactId,
                        userId,
                        guestToken);

            if (!allowed)
            {
                throw new HubException(
                    "Bạn không có quyền gửi tin nhắn.");
            }

            return await _chatMessageService
                .SendCustomerMessageAsync(
                    conversationId,
                    contactId,
                    content,
                    guestToken);
        }

        [Authorize]
        [PermissionAction(ActionType.Reply)]
        public async Task SendAdminMessage(
    long conversationId,
    string content)
        {
            var userId =
                Context.User?.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Bạn chưa đăng nhập.");

            var allowed =
                await _chatMessageService.CanAccessConversationAsync(
                    conversationId,
                    null,
                    userId,
                    isAdmin: true);

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền gửi tin nhắn.");

            var message =
                await _chatMessageService.SendAdminMessageAsync(
                    conversationId,
                    userId,
                    content,
                    Context.ConnectionAborted);

            await Clients.Group(
                ChatHubGroups.Conversation(conversationId))
                .SendAsync(
                    ChatHubEvents.MessageReceived,
                    message);
        }
        public async Task LeaveAdminConversation(long conversationId)
        {
            var userId =
        Context.User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

            if (string.IsNullOrEmpty(userId))
                return;

            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Conversation(conversationId));
            await _chatPresenceService.LeaveConversationAsync(
       userId,
       Context.ConnectionId,
       conversationId);

            await Clients.Group(
        ChatHubGroups.Conversation(conversationId)
    ).SendAsync(
        ChatHubEvents.AdminConvensationPresenceUpdated,
        new
        {
            userId,
            conversationId,
            isOnline = false
        });
        }
        public async Task LeaveConversation(long conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatHubGroups.Conversation(conversationId));
        }

        public async Task ConversationStatusUpdated(int conversationId, long inboxId, ChatConversationStatus status)
        {

            var dataSend = new
            {
                ConversationId = conversationId,
                InboxId = inboxId,
                Status = status.ToString(),
                UpdatedAt = DateTime.UtcNow,
            };

            await Clients.Group(ChatHubGroups.Inbox(inboxId)).SendAsync(ChatHubEvents.ConversationStatusUpdated, dataSend);
        }

        public async Task SendTyping(long conversationId, long contactId, string? guestToken, bool isTyping)
        {
            // TODO:
            // Validate contact + guestToken
            // Validate conversation ownership
            
            await Clients.Group(ChatHubGroups.Conversation(conversationId))
                .SendAsync(ChatHubEvents.Typing,
                    new
                    {
                        conversationId,
                        senderType = ChatSenderType.Contact,
                        isTyping
                    });
        }
        public async Task SendAdminTyping(
    long conversationId,
    bool isTyping)
        {
            if (!Context.User?.Identity?.IsAuthenticated ?? true)
                return;

            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new
                {
                    x.Id,
                    x.InboxId,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (conversation == null)
                return;

            // Resolved / Closed không được typing
            if (conversation.Status == ChatConversationStatus.Resolved ||
                conversation.Status == ChatConversationStatus.Closed)
            {
                return;
            }
            var userId =
        Context.User?.FindFirstValue(
            ClaimTypes.NameIdentifier);
            var allowed =
    await _chatMessageService.CanAccessConversationAsync(
        conversationId,
        null,
        userId,
        isAdmin: true,
        Context.ConnectionAborted);

            if (!allowed)
                return;
            // TODO:
            // Validate admin có quyền truy cập conversation/inbox này.

            await Clients
                .Group(ChatHubGroups.Conversation(conversationId))
                .SendAsync(
                    ChatHubEvents.Typing,
                    new
                    {
                        conversationId,
                        senderType = ChatSenderType.Admin,
                        isTyping
                    });
        }

        [Authorize]
        public async Task AdminMessageDelivered(long messageId)
        {
            var userId =
                Context.User?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Unauthenticated.");

            var cancellationToken =
                Context.ConnectionAborted;

            var message = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(
                    x => x.Id == messageId,
                    cancellationToken);

            if (message == null)
                return;

            // Admin chỉ được Delivered message do Customer gửi
            if (message.SenderType != ChatSenderType.Contact)
                return;

            // Không cho status đi lùi
            if (message.Status >= ChatMessageStatus.Delivered)
                return;

            var canAccess =
                await _chatMessageService
                    .CanAccessConversationAsync(
                        message.ConversationId,
                        null,
                        userId,
                        true,
                        cancellationToken);

            if (!canAccess)
                throw new HubException("Forbidden.");

            var now = DateTime.UtcNow;

            message.Status = ChatMessageStatus.Delivered;
            message.DeliveredAt = now;
            message.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await Clients.Group(
                ChatHubGroups.Conversation(
                    message.ConversationId))
                .SendAsync(
                    ChatHubEvents.MessageStatusUpdated,
                    new
                    {
                        ConversationId =
                            message.ConversationId,

                        MessageId =
                            message.Id,

                        Status =
                            ChatMessageStatus.Delivered
                    },
                    cancellationToken);
        }
        public async Task CustomerMessageDelivered(long messageId, long contactId, string? guestToken)
        {
            var cancellationToken = Context.ConnectionAborted;

            var message = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(
                    x => x.Id == messageId,
                    cancellationToken);

            if (message == null)
                return;

            // Customer chỉ được Delivered
            // message do Admin/Bot gửi
            if (
                message.SenderType != ChatSenderType.Admin &&
                message.SenderType != ChatSenderType.Bot)
            {
                return;
            }

            // Không cho status đi lùi
            if (message.Status >= ChatMessageStatus.Delivered)
                return;

           var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            var allowed =
                await _chatMessageService
                    .CanCustomerAccessConversationAsync(
                        message.ConversationId,
                        contactId,
                        userId,
                        guestToken,
                        cancellationToken);

            if (!allowed)
                throw new HubException("Forbidden.");

            var now = DateTime.UtcNow;

            message.Status = ChatMessageStatus.Delivered;
            message.DeliveredAt = now;
            message.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await Clients.Group(
                ChatHubGroups.Conversation(
                    message.ConversationId))
                .SendAsync(
                    ChatHubEvents.MessageStatusUpdated,
                    new
                    {
                        ConversationId =
                            message.ConversationId,

                        MessageId =
                            message.Id,

                        Status =
                            ChatMessageStatus.Delivered
                    },
                    cancellationToken);
        }

        public async Task MarkConversationAsRead(long conversationId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Unauthenticated.");
            var allowed =
      await _chatMessageService.CanAccessConversationAsync(
          conversationId,
          null,
          userId,
          isAdmin: true,
          Context.ConnectionAborted);

            if (!allowed)
                throw new HubException("Forbidden.");
            var messages = await _dbContext.ChatMessages
                .Where(x =>
                    x.ConversationId == conversationId &&
                    x.SenderType == ChatSenderType.Contact &&
                    x.Status < ChatMessageStatus.Read)
                .ToListAsync();

            if (messages.Count == 0)
                return;

            var now = DateTime.UtcNow;

            foreach (var message in messages)
            {
                message.Status = ChatMessageStatus.Read;
                message.ReadAt = now;
            }

            await _dbContext.SaveChangesAsync();

            await Clients.Group(ChatHubGroups.Conversation(conversationId)).SendAsync(ChatHubEvents.MessageStatusUpdated,
                    new
                    {
                        ConversationId = conversationId,
                        MessageIds = messages.Select(x => x.Id).ToList(),
                        Status = ChatMessageStatus.Read
                    });
        }


        [Authorize]
        [PermissionAction(ActionType.View)]
        public async Task<ChatConversationStatus?> OpenConversation(
    long conversationId)
        {
            var userId =
                Context.User?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("Bạn chưa đăng nhập.");

            var allowed =
                await _chatMessageService.CanAccessConversationAsync(
                    conversationId,
                    null,
                    userId,
                    isAdmin: true,
                    Context.ConnectionAborted);

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền truy cập conversation.");

            var result =
                await _chatMessageService.UpdateStatusAsync(
                    conversationId,
                    ChatConversationStatus.Open);

            if (!result.Succeeded)
                throw new HubException(result.Message);

            return ChatConversationStatus.Open;
        }
        [Authorize]
        public async Task MarkAllConversationsAsRead()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException(
                    "Bạn chưa đăng nhập."
                );
            }

            await _chatMessageService.MarkAllConversationsAsReadAsync();
        }

        [Authorize]
        [PermissionAction(ActionType.Reply)]
        public async Task<ChatConversationStatus?> ResolveConversation(
    long conversationId)
        {
            Console.WriteLine(
        $"[RESOLVE 1] conversationId={conversationId}");
            var userId =
                Context.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?
                    .Value;

            Console.WriteLine(
                $"[RESOLVE 2] userId={userId}");

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException(
                    "Bạn chưa đăng nhập.");

            var allowed =
                await _chatMessageService.CanAccessConversationAsync(
                    conversationId,
                    null,
                    userId,
                    isAdmin: true);
            Console.WriteLine(
        $"[RESOLVE 3] allowed={allowed}");

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền xử lý conversation.");

            var result =
                await _chatMessageService.ResolveConversationAsync(
                    conversationId,
                    userId,
                    Context.ConnectionAborted);
            Console.WriteLine(
      $"[RESOLVE 4] succeeded={result.Succeeded}, message={result.Message}");
            if (!result.Succeeded)
                throw new HubException(result.Message);
            Console.WriteLine(
     "[RESOLVE 5] SUCCESS");

            return ChatConversationStatus.Resolved;
        }
    }

}
