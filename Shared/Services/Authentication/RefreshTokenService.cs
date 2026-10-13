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
using Shared.UserValidation.Interface;
namespace Shared.Services.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IJwtService _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshTokenService(AppDbContext dbcontext,
            IJwtService jwtService, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<RefreshTokenResult> CreateAsync(string userId, TimeSpan lifeTime, CancellationToken cancellationToken = default)
        {
            var refreshToken = _jwtService.GenerateRefreshToken();
            var http = _httpContextAccessor.HttpContext;
            var now = DateTime.UtcNow;
            var result = new RefreshToken
            {
                UserId = userId,
                TokenHash = CommonHelper.Hash(refreshToken),
                CreatedAt = now,
                ExpiredAt = now.Add(lifeTime),
                Lifetime = lifeTime,
                UserAgent = http?.Request.Headers.UserAgent,
                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                IsRevoked = false
            };
            _dbcontext.RefreshTokens.Add(result);
            await _dbcontext.SaveChangesAsync(cancellationToken);

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
                return new RotateTokenResult
                {
                    Success = false,
                    Error = "Refresh token đã bị thu hồi"
                };
            }

            if (oldToken.ExpiredAt <= now)
            {
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
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiredAt = now.Add(oldToken.Lifetime),
                Lifetime = oldToken.Lifetime,
                UserAgent = http?.Request.Headers.UserAgent.ToString(),
                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
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

                return new RotateTokenResult
                {
                    Success = true,
                    RefreshToken = refreshToken,
                    RefreshTokenEntity = newToken,
                    User = oldToken.User
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync(cancellationToken);
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
