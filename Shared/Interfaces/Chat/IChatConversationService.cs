using Shared.Data.Entities.Chat;
using Shared.DTOs.Chat;
using Shared.Enums.Chat;
using Shared.Requests.Chat;
using Shared.Responses;

namespace Shared.Interfaces.Chat
{
    public interface IChatConversationService
    {
        Task<ChatInbox?> GetDefaultInboxAsync(CancellationToken cancellationToken = default); //INBOX CHƯA CÓ NGHIỆP VỤ RIÊNG NHIỀU ĐỂ CHUNG

        Task<ChatConversationDto> StartAdminConversationAsync(long inboxId, long contactId, CancellationToken cancellationToken = default);

        Task<StartChatResponse> StartClientConversationAsync(StartChatRequest request, string? userId, CancellationToken cancellationToken = default);

        Task<ChatConversationDto?> GetConversationAsync(long conversationId, CancellationToken cancellationToken = default);

        Task<object> GetConversationListAsync(long? inboxId, ChatConversationStatus? status, string? search, string? currentUserId, long? labelId = null,
            int limit = 30, DateTime? beforeLastMessageAt = null, long? beforeId = null, CancellationToken cancellationToken = default);

        Task<ChatConversationCountsDto> GetConversationCountsAsync(long? inboxId, string? search = null, string? assignedUserId = null,
            long? labelId = null, CancellationToken cancellationToken = default);
        
        Task<ChatConversationDetailDto?> GetConversationDetailAsync(long conversationId, CancellationToken cancellationToken = default);

        Task<ChatConversationContactDto?> GetConversationContactAsync(long conversationId, CancellationToken cancellationToken = default);

        Task<ServiceResult> UpdateStatusAsync(long conversationId, ChatConversationStatus status, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(long conversationId, long? contactId, string? guestToken, CancellationToken cancellationToken = default);

        Task<ServiceResult> MarkConversationAsReadAsync(long conversationId, long? contactId, string? guestToken, CancellationToken cancellationToken = default);
        
        Task<ServiceResult> MarkAllConversationsAsReadAsync(string userId, CancellationToken cancellationToken = default);

        Task<ServiceResult> ResolveConversationAsync(long conversationId, string userId, CancellationToken cancellationToken = default);

        Task<ChatConversationDto?> GetPreviousResolvedConversationAsync(long conversationId, long contactId, string? userId,
            string? guestToken, CancellationToken cancellationToken = default);
        
        Task<ChatConversationStatus?> GetCustomerConversationStatusAsync(long conversationId, CancellationToken cancellationToken = default);
    }
}
