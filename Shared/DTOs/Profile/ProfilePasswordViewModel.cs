using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Profile
{
    public class ProfilePasswordViewModel
    {
        public bool HasPassword { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}