using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Chat
{
    public interface IChatAuthorizationService
    {
        Task<bool> CanAccessConversationAsync(long conversationId, long? contactId, string? userId,
            bool isAdmin, CancellationToken cancellationToken = default);

        Task<bool> CanCustomerAccessConversationAsync(long conversationId, long? contactId, string? userId, 
            string? guestToken, CancellationToken cancellationToken = default);
    }
}
