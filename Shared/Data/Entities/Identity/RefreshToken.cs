using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity
{
    public class RefreshToken
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string TokenHash { get; set; } = null!;
        public string? ReplacedByTokenHash { get; set; }
        public string? JwtId { get; set; } = null!;
        public string? DeviceName { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public DateTime ExpiredAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public string? RevokedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public AppUser User { get; set; } = null!;

    }
}
