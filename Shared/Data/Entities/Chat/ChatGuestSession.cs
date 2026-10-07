using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Chat
{
    public class ChatGuestSession
    {
        public long Id { get; set; }

        public long ContactId { get; set; }

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }

        public ChatContact Contact { get; set; } = null!;

        [MaxLength(50)]
        public string? RevokedReason { get; set; }
    }
}
