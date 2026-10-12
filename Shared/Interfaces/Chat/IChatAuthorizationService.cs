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
