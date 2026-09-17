using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatTeam
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ChatTeamMember> Members { get; set; } = new List<ChatTeamMember>();

        public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();
    }
}
