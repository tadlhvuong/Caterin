using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Entities.Identity
{
    public class AppRole : IdentityRole
    {
        public int Level { get; set; }

        public bool IsSystem { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RolePermission> RolePermissions { get; set; } = [];

        public AppRole()
        {
        }

        public AppRole(string roleName, int level)
            : base(roleName)
        {
            Level = level;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
