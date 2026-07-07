
using Shared.Data.Entities.Identity;
using Shared.DTOs;

namespace Shared.Interfaces.AuthServices
{

    public interface IRefreshTokenService
    {
        /// <summary>
        /// Tạo refresh token
        /// </summary>
        Task<string> CreateAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy token trong DB
        /// </summary>
        Task<RefreshToken?> GetByTokenAsync(string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotate refresh token cũ sang token mới
        /// </summary>
        Task<RotateTokenResult> RotateAsync(RefreshToken refreshToken,
                CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke refresh token cũ 
        /// </summary>
        Task RevokeAsync(RefreshToken refreshToken,
                CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke tất cả refresh token cũ 
        /// </summary>
        Task RevokeAllUserTokensAsync(string userId,
                CancellationToken cancellationToken = default);
    }
}
