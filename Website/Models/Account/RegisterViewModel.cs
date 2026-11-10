using System.ComponentModel.DataAnnotations;

namespace Website.Models.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản.")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Tên tài khoản phải từ 3 đến 50 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Bạn phải đồng ý với Điều khoản sử dụng.")]
        public bool AcceptTerms { get; set; }
    }
}
