using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses.User
{
    public class UserListItemResponse
    {
        public string Id { get; set; } = null!;

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public List<string> Roles { get; set; } = [];

        public EntityStatus Status { get; set; }

        public bool IsActive { get; set; }

        public bool IsLocked { get; set; }
        public bool IsIdentityLocked { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }
    }
}
