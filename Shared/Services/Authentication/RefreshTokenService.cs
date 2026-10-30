using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Services.Log;
namespace Shared.Services.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IJwtService _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ILogger<RefreshToken> _logger;
        private readonly ISecurityLogger _securityLogger;

        public RefreshTokenService(AppDbContext dbcontext, IJwtService jwtService, IHttpContextAccessor httpContextAccessor,
            ILogger<RefreshToken> logger, ISecurityLogger securityLogger)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;

            _logger = logger;
            _securityLogger = securityLogger;
        }

        public async Task<RefreshTokenResult> CreateAsync(string userId, Guid sessionId, TimeSpan lifeTime, CancellationToken cancellationToken = default)
        {
            var refreshToken = _jwtService.GenerateRefreshToken();
            var http = _httpContextAccessor.HttpContext;
            var now = DateTime.UtcNow;
            var result = new RefreshToken
            {
                UserId = userId,
                SessionId = sessionId,
                TokenHash = CommonHelper.Hash(refreshToken),
                CreatedAt = now,
                ExpiredAt = now.Add(lifeTime),
                Lifetime = lifeTime,
                UserAgent = http?.Request.Headers.UserAgent,
                IpAddress = CommonHelper.GetClientIp(http),
                IsRevoked = false
            };
            _dbcontext.RefreshTokens.Add(result);
            await _dbcontext.SaveChangesAsync(cancellationToken);

            await _securityLogger.LogAsync(SecurityActionType.RefreshToken, true, $"Refresh token created. UserId={userId}");

            return new RefreshTokenResult { RefreshToken = refreshToken, ExpireAt = result.ExpiredAt };
        }

        public async Task<RefreshToken?> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenHash = CommonHelper.Hash(refreshToken);

            return await _dbcontext.RefreshTokens.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<RotateTokenResult> RotateAsync(RefreshToken oldToken, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            if (oldToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token rotation rejected because token is revoked. UserId={UserId}, TokenId={TokenId}",
                    oldToken.UserId, oldToken.Id);
                await _securityLogger.LogAsync(SecurityActionType.RefreshTokenReuse, false, $"Refresh token already revoked. UserId={oldToken.UserId}");
                
                return new RotateTokenResult
                {
                    Success = false,
                    Error = "Refresh token đã bị thu hồi"
                };
            }

            if (oldToken.ExpiredAt <= now)
            {
                _logger.LogInformation("Refresh token rotation rejected because token is expired. UserId={UserId}, TokenId={TokenId}",
                    oldToken.UserId, oldToken.Id);
                await _securityLogger.LogAsync(SecurityActionType.RefreshToken, false, $"Refresh token expired. UserId={oldToken.UserId}");
                
                return new RotateTokenResult
                {
                    Success = false,
                    Error = "Refresh token đã hết hạn"
                };
            }


            var http = _httpContextAccessor.HttpContext;
            var refreshToken = _jwtService.GenerateRefreshToken();
            var tokenHash = CommonHelper.Hash(refreshToken);

            var newToken = new RefreshToken
            {
                UserId = oldToken.UserId,
                SessionId = oldToken.SessionId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiredAt = now.Add(oldToken.Lifetime),
                Lifetime = oldToken.Lifetime,
                UserAgent = http?.Request.Headers.UserAgent.ToString(),
                IpAddress = CommonHelper.GetClientIp(http),
                IsRevoked = false
            };

            await using var transaction = await _dbcontext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Chỉ request đầu tiên thỏa điều kiện mới được rotate.
                var affectedRows = await _dbcontext.RefreshTokens.Where(x =>
                        x.Id == oldToken.Id && !x.IsRevoked && x.ExpiredAt > now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, "RefreshTokenRotated")
                        .SetProperty(x => x.ReplacedByTokenHash, tokenHash),
                        cancellationToken);

                if (affectedRows != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    _logger.LogWarning("Refresh token rotation rejected by conditional update. UserId={UserId}, TokenId={TokenId}",
                        oldToken.UserId, oldToken.Id);
                    await _securityLogger.LogAsync(SecurityActionType.RefreshTokenReuse, false, $"Refresh token rotation rejected. UserId={oldToken.UserId}");
                    
                    return new RotateTokenResult
                    {
                        Success = false,
                        Error = "Refresh token không còn hợp lệ hoặc đã được sử dụng"
                    };
                }

                // Chỉ tạo token mới khi thu hồi token cũ thành công.
                _dbcontext.RefreshTokens.Add(newToken);

                await _dbcontext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                // ExecuteUpdateAsync không đồng bộ entity đang được track.
                oldToken.IsRevoked = true;
                oldToken.RevokedAt = now;
                oldToken.RevokedReason = "RefreshTokenRotated";
                oldToken.ReplacedByTokenHash = tokenHash;

                _logger.LogInformation("Refresh token rotated successfully. UserId={UserId}, TokenId={TokenId}, NewTokenId={NewTokenId}",
                    oldToken.UserId, oldToken.Id, newToken.Id);
                await _securityLogger.LogAsync(SecurityActionType.RefreshToken, true, $"Refresh token rotated successfully. UserId={oldToken.UserId}");
                
                return new RotateTokenResult
                {
                    Success = true,
                    RefreshToken = refreshToken,
                    RefreshTokenEntity = newToken,
                    User = oldToken.User
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Unexpected error while rotating refresh token. UserId={UserId}, TokenId={TokenId}",
                    oldToken.UserId,  oldToken.Id);
                await _securityLogger.LogAsync(SecurityActionType.RefreshToken, false, $"Unexpected error while rotating refresh token. UserId={oldToken.UserId}, TokenId={oldToken.Id}");

                throw;
            }
        }

        public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync(cancellationToken);

            await _securityLogger.LogAsync(SecurityActionType.JwtRevoked, true, $"Refresh token revoked. UserId={refreshToken.UserId}");
        }
        public async Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _dbcontext.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked).ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbcontext.SaveChangesAsync(cancellationToken);
        }
    }
}
