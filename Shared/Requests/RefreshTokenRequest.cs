using System.ComponentModel.DataAnnotations;

namespace Shared.Requests
{
    public class RefreshTokenRequest
    {
        [Required]
        public string AccessToken { get; set; } = default!;
        [Required]
        public string RefreshToken { get; set; } = default!;
    }
}
