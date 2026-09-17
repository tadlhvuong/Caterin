using Shared.Data.Entities.Identity;
using Shared.Enums.Chat;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatConversation
    {
        public long Id { get; set; }

        public long InboxId { get; set; }

        public long ContactId { get; set; }

        public string? AssignedUserId { get; set; }

        public int? TeamId { get; set; }

        public ChatConversationStatus Status { get; set; }

        public ChatConversationPriority Priority { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; }

        public DateTime LastMessageAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ChatInbox Inbox { get; set; } = null!;

        public ChatContact Contact { get; set; } = null!;

        public AppUser? AssignedUser { get; set; }

        public ChatTeam? Team { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatConversationLabel> Labels { get; set; }  = new List<ChatConversationLabel>();
    }
}
