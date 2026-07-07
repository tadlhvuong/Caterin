using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "{0} not null")]
        [StringLength(32, ErrorMessage = "{0} Length minimum {2} characters", MinimumLength = 3)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "{0} not null")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "RememberMe")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class LoginExViewModel
    {
        public string Provider { get; set; }

        public string ReturnUrl { get; set; }
    }
    public class RegisterViewModel
    {
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
    }
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "{0} not null")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    public class MemberResetPasswordModel
    {
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

        public string Code { get; set; }
        public string UserId { get; set; }
    }
    public class ExternalLoginListViewModel
    {
        public string? ReturnUrl { get; set; }
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

}
