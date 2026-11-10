using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class UserSession
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = null!;
        [Required]
        [MaxLength(256)]
        public string SecurityStamp { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }

        public string? UserAgent { get; set; }

        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastActivityAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RevokedReason { get; set; }

        public string? RevokedById { get; set; }

        public AppUser User { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();
    }
}
