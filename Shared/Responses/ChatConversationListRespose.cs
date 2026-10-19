using Shared.DTOs.Chat;

namespace Shared.Responses
{
    public class ChatConversationListResponse
    {
        public List<ChatConversationListItemDto> Items { get; set; } = [];

        public bool HasMore { get; set; }

        public bool? BeforeIsPending { get; set; }

        public DateTime? OldestLastMessageAt { get; set; }

        public long? OldestId { get; set; }
    }
}
