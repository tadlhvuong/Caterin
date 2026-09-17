using Shared.Enums.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Notification
{
    public interface IChatRealtimeNotifier
    {
        Task NotifyConversationStatusUpdatedAsync(long conversationId, long inboxId,
        ChatConversationStatus status, DateTime? updatedAt);

        Task NotifyConversationReadAsync(
        long conversationId,
        long inboxId,
        CancellationToken cancellationToken = default);
    }
}
