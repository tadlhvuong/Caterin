using Shared.Data.Entities.Chat;
using Shared.DTOs.Chat;
using Shared.Enums.Chat;
using Shared.Requests.Chat;
using Shared.Responses;
using System.Threading.Tasks;

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
        Task<object> GetConversationListAsync(
    long? inboxId,
    string? currentUserId,
    int limit = 30,
    DateTime? beforeLastMessageAt = null,
    long? beforeId = null,
    CancellationToken cancellationToken = default);
        Task<ChatConversationCountsDto> GetConversationCountsAsync(long? inboxId,
     string? search = null,
     string? assignedUserId = null,
     long? labelId = null,
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
        Task<object> GetAdminMessagesAsync(
    long conversationId,
    int limit,
    long? before,
    CancellationToken cancellationToken = default);
        Task<object> GetCustomerMessagesAsync(
    long conversationId,
    int limit,
    long? before,
    CancellationToken cancellationToken = default);


        //UPDATE STATUS MESSAGE
        Task<ServiceResult> UpdateStatusAsync(long conversationId, ChatConversationStatus status, 
            CancellationToken cancellationToken = default);
       Task<int> GetUnreadCountAsync(long conversationId, long? contactId, string? guestToken, 
           CancellationToken cancellationToken = default);
        Task<ServiceResult> MarkConversationAsReadAsync(long conversationId, long? contactId,
            string? guestToken, CancellationToken cancellationToken = default);
    }
}
