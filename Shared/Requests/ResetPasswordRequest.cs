using System.ComponentModel.DataAnnotations;

namespace Shared.Requests
{
    public sealed class ResetPasswordRequest
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
