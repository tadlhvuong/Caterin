using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "{0} không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(256, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} không được để trống.")]
        [DataType(DataType.Password)]
        [StringLength(128, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class LoginExViewModel
    {
        public string Provider { get; set; }

        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; } = false;
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "{0} not null")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "{0} not null")]
        [EmailAddress(ErrorMessage = "{0} invalid")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "{0} not null")]
        [StringLength(100, ErrorMessage = "{0} must be at least {2} characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "{0} not null")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        [Compare("Password", ErrorMessage = "NotMatchConfirmPass")]
      
        public string ConfirmPassword { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với Điều khoản sử dụng.")]
        public bool AcceptTerms { get; set; }
    }

    public sealed class ExternalRegisterViewModel
    {
        [Required(ErrorMessage = "{0} not null")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "UserName")]
        public string UserName { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Bạn phải đồng ý với Điều khoản sử dụng.")]
        public bool AcceptTerms { get; set; }

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Bạn phải đồng ý với Chính sách bảo mật.")]
        public bool AcceptPrivacy { get; set; }

        // Chỉ dùng để hiển thị, không dùng làm dữ liệu tin cậy khi POST.
        public string? Email { get; set; }

        public string? Provider { get; set; }
        public string? ReturnUrl { get; set; }
    }
    public class ResendConfirmEmailViewModel
    {
        public string Email { get; set; } = default!;
        public string? Message { get; set; }
    }
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(
            100,
            MinimumLength = 12,
            ErrorMessage = "Mật khẩu phải có từ {2} đến {1} ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare(
            nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Liên kết đặt lại mật khẩu không hợp lệ.")]
        public string Key { get; set; } = string.Empty;
    }

    public sealed class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    public class ConfirmEmailActionViewModel
    {
        [Required]
        public string Key { get; set; } = string.Empty;
    }
    public class ExternalLoginListViewModel
    {
        public string? ReturnUrl { get; set; }
        public bool RememberMe { get; set; } = false;
    }
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessage = "{0} không được để trống")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    public class AccountLocationModel
    {
        public string? status { get; set; }
        public string? continent { get; set; }
        public string? country { get; set; }
        public string? regionName { get; set; }
        public string? city { get; set; }
        public string? district { get; set; }
        public string? zip { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public string? isp { get; set; }
        public string? query { get; set; }
    }

    public sealed class FeedbackViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string ButtonText { get; set; } = "Đăng nhập";

        public string ButtonUrl { get; set; } = "/admin/auth/login";

        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonUrl { get; set; }

        public FeedbackType Type { get; init; }

        public bool Success { get; set; }
    }

}
