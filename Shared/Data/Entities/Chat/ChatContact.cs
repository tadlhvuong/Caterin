using Shared.Data.Entities.Identity;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatContact
    {
        public long Id { get; set; }

        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? GuestToken { get; set; }

        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public string? CustomAttributesJson { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public AppUser? User { get; set; }

        public ICollection<ChatContactInbox> Inboxes { get; set; } = new List<ChatContactInbox>();

        public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
