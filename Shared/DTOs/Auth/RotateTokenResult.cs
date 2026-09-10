using Shared.Data.Entities.Identity;

namespace Shared.DTOs
{
    public class RotateTokenResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string RefreshToken { get; set; } = default!;

        public RefreshToken RefreshTokenEntity { get; set; } = default!;

        public AppUser User { get; set; } = default!;
    }
}
