using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Chat
{
    public interface IChatPresenceService
    {
        Task AddConnectionAsync(
            string userId,
            string connectionId);

        Task<bool> RemoveConnectionAsync(
            string userId,
            string connectionId);

        bool IsOnline(string userId);

        IReadOnlyCollection<string> GetOnlineAdminIds();
        bool IsAnyOnline();
    }
}
