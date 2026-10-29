using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models.User
{
    public class UpdatePersonalViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? ProvinceCode { get; set; }

        [StringLength(20)]
        public string? WardCode { get; set; }

        public DateTime? BirthDate { get; set; }

        public Gender? Gender { get; set; }

        public List<UserSocialLinkInputModel> SocialLinks { get; set; } = new();
    }

    public class UserSocialLinkInputModel
    {
        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty;

        [StringLength(2048)]
        [Url]
        public string? Url { get; set; }
    }
}
