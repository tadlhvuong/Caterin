using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Core;
using Shared.Constants.Permission;
using Shared.Data.Context;
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
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (Context.User?.Identity?.IsAuthenticated == true &&
                !string.IsNullOrWhiteSpace(userId))
            {
                await _chatPresenceService.AddConnectionAsync(
                    userId,
                    Context.ConnectionId);

                await Clients.All.SendAsync(
                    ChatHubEvents.AdminOnline,
                    new
                    {
                        AdminId = userId
                    });
            }

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(
    Exception? exception)
        {
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (Context.User?.Identity?.IsAuthenticated == true &&
                !string.IsNullOrWhiteSpace(userId))
            {
                var isOffline =
                    await _chatPresenceService.RemoveConnectionAsync(
                        userId,
                        Context.ConnectionId);

                if (isOffline)
                {
                    await Clients.All.SendAsync(
                        ChatHubEvents.AdminOffline,
                        new
                        {
                            AdminId = userId
                        });
                }
            }

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
        public async Task JoinConversation(
    long conversationId,
    long contactId,
    string? guestToken)
        {
            var userId = Context.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            var allowed =
                await _chatMessageService.CanCustomerAccessConversationAsync(
                    conversationId,
                    contactId,
                    userId,
                    guestToken);

            if (!allowed)
            {
                throw new HubException(
                    "Bạn không có quyền truy cập conversation này.");
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Conversation(conversationId));
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
        public async Task SendCustomerMessage(
    long conversationId,
    long contactId,
    string content,
    string? guestToken)
        {
            var userId = Context.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            var allowed =
                await _chatMessageService.CanCustomerAccessConversationAsync(
                    conversationId,
                    contactId,
                    userId,
                    guestToken);

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền gửi tin nhắn.");

            await _chatMessageService.SendCustomerMessageAsync(
                conversationId,
                contactId,
                content,
                guestToken);
        }

        [Authorize]
        [PermissionAction(ActionType.Reply)]
        public async Task SendAdminMessage(long conversationId, string content)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException(
                    "Bạn chưa đăng nhập.");

            var allowed =
                await _chatMessageService.CanAccessConversationAsync(
                    conversationId,
                    null,
                    userId,
                    isAdmin: true);

            if (!allowed)
                throw new HubException(
                    "Bạn không có quyền gửi tin nhắn.");

            var message = await _chatMessageService
                .SendAdminMessageAsync(
                    conversationId,
                    userId,
                    content, Context.ConnectionAborted);
            await Clients.Group(
       ChatHubGroups.Conversation(conversationId))
       .SendAsync(
           ChatHubEvents.MessageReceived,
           message);
        }
        public async Task LeaveAdminConversation(long conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                ChatHubGroups.Conversation(conversationId));
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
        public async Task SendAdminTyping(long conversationId, bool isTyping)
        {
            if (!Context.User?.Identity?.IsAuthenticated ?? true)
                return;

            var conversation = await _dbContext.ChatConversations
                .AsNoTracking()
                .Where(x => x.Id == conversationId)
                .Select(x => new
                {
                    x.Id,
                    x.InboxId
                })
                .FirstOrDefaultAsync();

            if (conversation == null)
                return;

            // TODO:
            // Validate admin có quyền truy cập conversation/inbox này
            // nếu hệ thống của bạn có Team/Assignment/Permission riêng.

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
    }

}
