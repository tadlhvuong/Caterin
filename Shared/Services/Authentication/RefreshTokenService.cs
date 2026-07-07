using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Auth;
using Shared.Interfaces.AuthServices;
using Shared.Responses;
using Shared.UserValidation.Interface;
using System.Security.Claims;
namespace Shared.Services.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _dbcontext;

        private readonly IJwtService
            _jwtService;

        private readonly JwtSetting _jwtSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IUserValidationService _userValidationService;

        public RefreshTokenService(
            AppDbContext dbcontext,
            IJwtService jwtService,
            IOptions<JwtSetting> JwtSetting, IHttpContextAccessor httpContextAccessor,
            IUserValidationService userValidationService)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _jwtSettings = JwtSetting.Value;
            _httpContextAccessor = httpContextAccessor;
            _userValidationService = userValidationService;
        }

        public async Task<string> CreateAsync(string userId,
        CancellationToken cancellationToken = default)
        {
            var refreshToken = _jwtService.GenerateRefreshToken();

            var http = _httpContextAccessor.HttpContext;

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = CommonHelper.Hash(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                UserAgent = http?.Request.Headers.UserAgent,
                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                IsRevoked = false
            };
            _dbcontext.RefreshTokens.Add(entity);
            await _dbcontext.SaveChangesAsync(cancellationToken);

            return refreshToken;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default)
        {
            var tokenHash = CommonHelper.Hash(refreshToken);

            return await _dbcontext.RefreshTokens.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<RotateTokenResult> RotateAsync(RefreshToken oldToken,
        CancellationToken cancellationToken = default)
        {
            oldToken.IsRevoked = true;
            oldToken.RevokedAt = DateTime.UtcNow;
            var refreshToken = _jwtService.GenerateRefreshToken();
            var http = _httpContextAccessor.HttpContext;
            var newToken = new RefreshToken
            {
                UserId = oldToken.UserId,
                TokenHash = CommonHelper.Hash(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                UserAgent = http?.Request.Headers.UserAgent,
                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                IsRevoked = false
            };
            _dbcontext.RefreshTokens.Add(newToken);
            oldToken.ReplacedByTokenHash = newToken.TokenHash;
            oldToken.RevokedAt = DateTime.UtcNow;
            oldToken.RevokedReason = "RefreshTokenRotated";

            await _dbcontext.SaveChangesAsync(cancellationToken);

            return new RotateTokenResult
            {
                RefreshToken = refreshToken,
                RefreshTokenEntity = newToken,
                User = oldToken.User
            };
        }

        public async Task RevokeAsync(RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAllUserTokensAsync(
        string userId,
        CancellationToken cancellationToken = default)
        {
            var tokens = await _dbcontext.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbcontext.SaveChangesAsync(cancellationToken);
        }
    }
}
