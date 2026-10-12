using Shared.DTOs.Chat;

namespace Shared.Interfaces.Chat
{
    public interface IChatMessageService
    {
        Task<CustomerSendMessageResult> SendCustomerMessageAsync(long? conversationId, long? contactId, long inboxId,
            string content, string? guestToken, string? userId, string? uploadSessionToken, List<long>? uploadIds, CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SendAdminMessageAsync(long conversationId, string userId, string content, CancellationToken cancellationToken = default);

        Task<object> GetAdminMessagesAsync(long conversationId, int limit, long? before, CancellationToken cancellationToken = default);
        
        Task<object> GetCustomerMessagesAsync(long conversationId, int limit, long? before, CancellationToken cancellationToken = default);
    }
}
