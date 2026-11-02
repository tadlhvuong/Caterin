namespace Shared.DTOs.User
{
    public class UserDetailsDto
    {
        public string Id { get; set; } = string.Empty;
        public string? AvatarStoragePath { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }

        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }

        public List<string> Roles { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; }
    }
}
