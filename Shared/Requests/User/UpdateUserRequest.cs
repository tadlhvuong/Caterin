using System.ComponentModel.DataAnnotations;

namespace Shared.Requests.User
{
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(256)]
        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }

}
