using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Profile
{

    public class UserProfileDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string DisplayName { get; set; } = string.Empty;

        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        public string? AvatarUrl { get; set; }

        public bool SavedSuccessfully { get; set; }
    }
}
