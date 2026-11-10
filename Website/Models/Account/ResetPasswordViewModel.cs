using System.ComponentModel.DataAnnotations;

namespace Website.Models.Account
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
