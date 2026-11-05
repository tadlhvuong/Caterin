using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models.User
{
    public class UpdateUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public bool IsActive { get; set; }
    }
}
