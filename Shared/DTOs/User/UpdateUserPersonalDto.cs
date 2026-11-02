using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.User
{
    public class UpdateUserPersonalDto
    {
        public string? DisplayName { get; set; }
        public string? Address { get; set; }
        public string? ProvinceCode { get; set; }
        public string? WardCode { get; set; }
        public DateTime? BirthDate { get; set; }
        public Gender? Gender { get; set; }

        public List<UserSocialLinkDto> SocialLinks { get; set; } = new();
    }
}
