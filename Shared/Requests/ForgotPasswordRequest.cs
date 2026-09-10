using System.ComponentModel.DataAnnotations;

namespace Shared.Requests
{
    public sealed class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
