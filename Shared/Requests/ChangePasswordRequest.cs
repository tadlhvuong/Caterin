using System.ComponentModel.DataAnnotations;

namespace Shared.Requests
{
    using System.ComponentModel.DataAnnotations;

    public sealed class ChangePasswordRequest
    {
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
