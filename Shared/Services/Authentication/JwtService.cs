using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Configurations;
using Shared.Constants.Permission;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Auth;
using Shared.Interfaces.AuthServices;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Services.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtSetting _jwtSettings;

    private readonly UserManager<AppUser>
        _userManager;

    public JwtService(
        IOptions<JwtSetting> jwtSettings,
        UserManager<AppUser> userManager)
    {
        _jwtSettings = jwtSettings.Value;

        _userManager = userManager;
    }

    public string GenerateAccessToken(AppUser user,
     IEnumerable<string> roles)
    {
        
        var claims = CreateClaims(user, roles);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        var credentials = new SigningCredentials( key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,

            audience: _jwtSettings.Audience,

            claims: claims,

            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),

            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
    private List<Claim> CreateClaims(AppUser user,
     IEnumerable<string> roles)
    {
        var claims =
            new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id),
                new( JwtRegisteredClaimNames.Sub,  user.Id),
                new(ClaimConstants.UserId, user.Id ?? string.Empty),
                new(ClaimConstants.UserName, user.UserName ?? string.Empty),
                new Claim(ClaimConstants.SecurityStamp, user.SecurityStamp ?? string.Empty),
                new(ClaimConstants.PermissionVersion, user.PermissionVersion.ToString()),
                new( JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
    public bool IsTokenExpired(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            return jwt.ValidTo <= DateTime.UtcNow;
        }
        catch (Exception)
        {

            return true;
        }
    }
    //Hiện tại không dùng. Hàm này dùng khi thêm API nội bộ nhận JWT dưới dạng string. Do giờ đang dùng UserValidationContext save HttpContext.Items
    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = _jwtSettings.SecretKey;
        var principal = tokenHandler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // quan trọng: refresh token có thể expired access token
                ValidateIssuerSigningKey = true,

                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            },
            out SecurityToken validatedToken);

        return principal;
    }
}
