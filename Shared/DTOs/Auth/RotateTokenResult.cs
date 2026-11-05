using Shared.Data.Entities.Identity;
using Shared.Enums;

namespace Shared.DTOs
{
    public class RotateTokenResult
    {
        public bool Success { get; set; }

        public RotateTokenError ErrorCode { get; set; } = RotateTokenError.None;

        public string? Error { get; set; }

        public string RefreshToken { get; set; } = default!;

        public RefreshToken RefreshTokenEntity { get; set; } = default!;

        public AppUser User { get; set; } = default!;
    }
}
