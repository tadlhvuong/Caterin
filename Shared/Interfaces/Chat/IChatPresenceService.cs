using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Chat
{
    public interface IChatPresenceService
    {
        Task<bool> AddConnectionAsync(string userId, string connectionId);

        Task<bool> RemoveConnectionAsync(
            string userId,
            string connectionId);

        bool IsOnline(string userId);

        IReadOnlyCollection<string> GetOnlineAdminIds();
        bool IsAnyOnline();

        Task<bool> JoinConversationAsync(
        string userId,
        string connectionId,
        long conversationId);

        Task<bool> LeaveConversationAsync(
            string userId,
            string connectionId,
            long conversationId);

        bool IsConversationActive(
            long conversationId);
        //OnDisconnectedAsync() có thể xảy ra mà không đi qua LeaveAdminConversation
        Task RemoveConnectionFromConversationsAsync(string connectionId);
    }
}
