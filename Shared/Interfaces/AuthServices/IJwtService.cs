using Shared.Data.Entities.Identity;
using System.Security.Claims;

namespace Shared.Interfaces.AuthServices
{
    public interface IJwtService
    {
        /// <summary>
        /// Tạo access token
        /// </summary>
        string GenerateAccessToken(AppUser user, IEnumerable<string> roles);

        /// <summary>
        /// Tạo refresh token
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Kiểm tra token còn hạn?
        /// </summary>
        bool IsTokenExpired(string token);

        /// <summary>
        /// Tự parse JWT.
        /// </summary>
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}


