using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public class UserResponseDto
    {
        public string Id { get; set; } = null!;

        public string? UserName { get; set; }
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Avatar { get; set; }

        // status
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public EntityStatus Status { get; set; }

        // audit
        public DateTime CreatedAt { get; set; }
        public string? CreatedById { get; set; }

        public DateTime? LastLogin { get; set; }
        public string? LastLoginIp { get; set; }

        public DateTime? LastUpdate { get; set; }

        // UI computed (không nên đưa logic NotMapped)
        public string? AvatarImg { get; set; }

        // role (rất quan trọng cho RBAC)
        public List<string> Roles { get; set; } = new();

        // permission (optional nhưng rất mạnh)
        public List<string> Permissions { get; set; } = new();
    }
}
