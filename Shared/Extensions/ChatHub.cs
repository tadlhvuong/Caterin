using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Core;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.Chat;
using System.Security.Claims;

namespace Shared.Extensions
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _dbContext;
        private readonly IChatMessageService _chatMessageService;
        public ChatHub(AppDbContext dbContext, IChatMessageService chatMessageService)
        {
            _dbContext = dbContext;
            _chatMessageService = chatMessageService;
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
    }
}
