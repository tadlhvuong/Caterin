using Shared.Enums;

namespace Shared.DTOs
{
    public class UserResponseDto
    {
        public string Id { get; set; } = null!;

        public string? UserName { get; set; }
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Avatar { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public EntityStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedById { get; set; }

        public DateTime? LastLogin { get; set; }
        public string? LastLoginIp { get; set; }

        public DateTime? LastUpdate { get; set; }

        public string? AvatarImg { get; set; }

        public List<string> Roles { get; set; } = new();

        public List<string> Permissions { get; set; } = new();
    }
}
