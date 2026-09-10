using Shared.Data.Entities.Identity;
using System.Security.Claims;

namespace Shared.Interfaces.AuthServices
{
    public interface IJwtService
    {
        /// <summary>
        /// Tạo access token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        string GenerateAccessToken(AppUser user, IEnumerable<string> roles);
        /// <summary>
        /// Tạo refresh token
        /// </summary>
        /// <returns></returns>
        string GenerateRefreshToken();
        /// <summary>
        /// Kiểm tra token còn hạn?
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        bool IsTokenExpired(string token);
        /// <summary>
        /// Tự parse JWT
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}


