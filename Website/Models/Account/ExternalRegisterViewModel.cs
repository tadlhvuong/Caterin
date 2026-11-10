using System.ComponentModel.DataAnnotations;

namespace Website.Models.Account
{
    public class ExternalRegisterViewModel
    {
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Tên tài khoản phải từ 3 đến 50 ký tự.")]
        public string? UserName { get; set; }

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Bạn phải đồng ý với Điều khoản sử dụng.")]
        public bool AcceptTerms { get; set; }

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Bạn phải đồng ý với Chính sách bảo mật.")]
        public bool AcceptPrivacy { get; set; }
    }
}