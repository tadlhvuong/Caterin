using Shared.Data.Entities.Identity;

namespace Shared.Data.Entities.Chat
{
    public class ChatTeamMember
    {
        public int TeamId { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public ChatTeam Team { get; set; } = null!;

        public AppUser User { get; set; } = null!;
    }
}
