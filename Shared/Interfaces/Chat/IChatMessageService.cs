using Shared.Data.Entities.Chat;
using Shared.DTOs.Chat;
using Shared.Enums.Chat;
using Shared.Requests.Chat;
using Shared.Responses;

namespace Shared.Interfaces.Chat
{
    public interface IChatMessageService
    {
        Task<ChatConversationDto> StartConversationAsync(
            StartChatRequest request,
            string? userId,
            CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SendCustomerMessageAsync(
            long conversationId,
            long contactId,
            string content,
            string? guestToken,
            CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SendAdminMessageAsync(
            long conversationId,
            string userId,
            string content,
            CancellationToken cancellationToken = default);

        Task<ChatConversationDto?> GetConversationAsync(
            long conversationId,
            CancellationToken cancellationToken = default);
        Task<List<ChatConversationListItemDto>> GetConversationsAsync(
    long inboxId, string? currentUserId,
    CancellationToken cancellationToken = default);

        Task<bool> CanAccessConversationAsync(
            long conversationId,
            long? contactId,
            string? userId,
            bool isAdmin,
            CancellationToken cancellationToken = default);
        Task<bool> CanCustomerAccessConversationAsync(
    long conversationId,
    long contactId,
    string? userId,
    string? guestToken,
    CancellationToken cancellationToken = default);

        Task<ChatInbox?> GetDefaultInboxAsync(
        CancellationToken cancellationToken = default);

        Task<ChatConversationDetailDto?> GetConversationDetailAsync(
            long conversationId,
            CancellationToken cancellationToken = default);
        Task<ChatConversationContactDto?> GetConversationContactAsync(
    long conversationId,
    CancellationToken cancellationToken = default);
        Task<List<ChatMessageDto>> GetMessagesAsync(
        long conversationId,
        CancellationToken cancellationToken = default);

        Task<List<ChatMessageDto>> GetCustomerMessagesAsync(
    long conversationId,
    CancellationToken cancellationToken = default);


        //UPDATE STATUS MESSAGE
        Task<ServiceResult> UpdateStatusAsync(int conversationId, ChatConversationStatus status, 
            CancellationToken cancellationToken = default);
       Task<int> GetUnreadCountAsync(long conversationId, long? contactId, string? guestToken, 
           CancellationToken cancellationToken = default);
        Task<ServiceResult> MarkConversationAsReadAsync(long conversationId, long? contactId,
            string? guestToken, CancellationToken cancellationToken = default);
    }
}
