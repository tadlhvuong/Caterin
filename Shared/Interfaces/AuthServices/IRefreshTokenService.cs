using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Auth;

namespace Shared.Interfaces.AuthServices
{
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Tạo refresh token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="lifeTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<RefreshTokenResult> CreateAsync(string userId, TimeSpan lifeTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy token trong DB
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<RefreshToken?> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotate refresh token cũ sang token mới
        /// </summary>
        Task<RotateTokenResult> RotateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke refresh token cũ 
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revoke tất cả refresh token cũ 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default);
    }
}
