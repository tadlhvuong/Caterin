using Shared.Enums;

namespace Shared.DTOs.User
{
    public class UserPersonalDetailsDto
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }

        public string? Address { get; set; }
        public string? ProvinceCode { get; set; }
        public string? WardCode { get; set; }

        public DateTime? BirthDate { get; set; }
        public Gender? Gender { get; set; }

        public List<UserSocialLinkDto> SocialLinks { get; set; } = new();
    }
}
