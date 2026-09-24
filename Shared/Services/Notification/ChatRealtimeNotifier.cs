using Microsoft.AspNetCore.SignalR;
using Shared.Constants.Core;
using Shared.Enums.Chat;
using Shared.Extensions;
using Shared.Interfaces.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Notification
{
    public class ChatRealtimeNotifier : IChatRealtimeNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyConversationStatusUpdatedAsync(
     long conversationId,
     long inboxId,
     ChatConversationStatus status,
     DateTime? updatedAt)
        {
            await _hubContext.Clients
                .Group(ChatHubGroups.Inbox(inboxId))
                .SendAsync(
                    ChatHubEvents.ConversationStatusUpdated,
                    new
                    {
                        InboxId = inboxId,
                        ConversationId = conversationId,
                        Status = status.ToString(),
                        UpdatedAt = updatedAt
                    });
        }

        public async Task NotifyConversationReadAsync(
    long conversationId,
    long inboxId,
    CancellationToken cancellationToken = default)
        {
            await _hubContext
                .Clients
                .Group(
                    ChatHubGroups.Conversation(
                        conversationId))
                .SendAsync(
                    ChatHubEvents.ConversationRead,
                    new
                    {
                        conversationId
                    },
                    cancellationToken);

            await _hubContext
                .Clients
                .Group(
                    ChatHubGroups.Inbox(inboxId))
                .SendAsync(
                    ChatHubEvents.ConversationRead,
                    new
                    {
                        conversationId
                    },
                    cancellationToken);
        }
    }
}
