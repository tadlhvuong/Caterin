using Shared.Enums.Chat;

namespace Shared.DTOs.Chat
{
    public class ChatConversationDto
    {
        public long Id { get; set; }

        public long InboxId { get; set; }

        public long ContactId { get; set; }

        public  string?  GuestToken { get; set; }
        public string? ContactName { get; set; }

        public string? ContactAvatar { get; set; }

        public string? AssignedUserId { get; set; }

        public ChatConversationStatus Status { get; set; }

        public ChatConversationPriority Priority { get; set; }

        public string? Subject { get; set; }

        public DateTime LastMessageAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
