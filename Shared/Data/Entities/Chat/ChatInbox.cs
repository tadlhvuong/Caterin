using Shared.Enums.Chat;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatInbox
    {
        public long Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        public ChatChannelType ChannelType { get; set; }

        public bool IsActive { get; set; }

        public string? SettingsJson { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();

        public ICollection<ChatContactInbox> Contacts { get; set; } = new List<ChatContactInbox>();
    }
}
