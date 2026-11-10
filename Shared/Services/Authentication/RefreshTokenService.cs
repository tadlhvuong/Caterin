using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants.Core;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Services.Log;
using System.Security.Cryptography;
namespace Shared.Services.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IJwtService _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataProtector _refreshTokenProtector;
        private readonly IDataProtector _deviceProtector;
        private readonly ILogger<RefreshToken> _logger;
        private readonly ILogSafetyService _logSafetyService;
        private readonly JwtSetting _jwtSettings;

        public RefreshTokenService(AppDbContext dbcontext, IDataProtectionProvider dataProtectionProvider,
    IJwtService jwtService, IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshToken> logger, ILogSafetyService logSafetyService, IOptions<JwtSetting> jwtOptions)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;

            _refreshTokenProtector = dataProtectionProvider.CreateProtector(DataProtectionPurposes.RefreshTokenRotation);

            _deviceProtector = dataProtectionProvider.CreateProtector(DataProtectionPurposes.DeviceIdentity);

            _logger = logger;
            _logSafetyService = logSafetyService;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<RefreshTokenResult> CreateAsync(string userId, Guid sessionId, TimeSpan lifeTime, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            if (sessionId == Guid.Empty)
                throw new ArgumentException("SessionId không hợp lệ.", nameof(sessionId));

            if (lifeTime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifeTime));

            cancellationToken.ThrowIfCancellationRequested();

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
                UserAgent = http?.Request.Headers.UserAgent.ToString(),
                IpAddress = CommonHelper.GetClientIp(http),
                IsRevoked = false
            };
            _dbcontext.RefreshTokens.Add(result);
            await _dbcontext.SaveChangesAsync(cancellationToken);
            
            return new RefreshTokenResult { RefreshToken = refreshToken, ExpireAt = result.ExpiredAt };
        }

        public async Task<RefreshToken?> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenHash = CommonHelper.Hash(refreshToken);

            return await _dbcontext.RefreshTokens.Include(x => x.User).Include(x => x.Session)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }


        public async Task<RotateTokenResult> RotateAsync(
            RefreshToken oldToken,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            await using var transaction =
                await _dbcontext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Đọc trạng thái mới nhất từ database
                var currentToken = await _dbcontext.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == oldToken.Id,
                        cancellationToken);

                if (currentToken == null)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = RotateTokenError.TokenNotFound,
                        Error = "Refresh token không tồn tại."
                    };
                }

                // 2. Token đã bị thu hồi
                if (currentToken.IsRevoked)
                {
                    if (currentToken.RevokedReason != "RefreshTokenRotated")
                    {
                        await transaction.RollbackAsync(cancellationToken);

                        return new RotateTokenResult
                        {
                            Success = false,
                            ErrorCode = RotateTokenError.TokenRevoked,
                            Error = "Refresh token đã bị thu hồi."
                        };
                    }

                    var retryResult = await GetRetryResultAsync(
                        currentToken,
                        oldToken.User,
                        now,
                        cancellationToken);

                    if (retryResult != null)
                    {
                        if (!retryResult.Success)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return retryResult;
                        }

                        await transaction.CommitAsync(cancellationToken);
                        return retryResult;
                    }

                    // Retry window đã hết: xử lý reuse
                    await RevokeSessionForReuseAsync(
                        currentToken.UserId,
                        currentToken.SessionId,
                        now,
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    await _logSafetyService.TryLogSecurityAsync(
                        SecurityActionType.RefreshTokenReuse,
                        false,
                        $"Refresh token reuse detected. UserId={currentToken.UserId}, SessionId={currentToken.SessionId}");

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = RotateTokenError.ReuseDetected,
                        Error = "Refresh token đã được sử dụng lại."
                    };
                }

                // 3. Kiểm tra hạn token
                if (currentToken.ExpiredAt <= now)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = RotateTokenError.TokenExpired,
                        Error = "Refresh token đã hết hạn."
                    };
                }

                // 4. Kiểm tra Lifetime
                if (currentToken.Lifetime <= TimeSpan.Zero)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = RotateTokenError.InvalidLifetime,
                        Error = "Refresh token lifetime không hợp lệ."
                    };
                }

                // 5. Tính thời hạn token mới
                var newExpiry = now.Add(currentToken.Lifetime);

                // 6. Kiểm tra và cập nhật Session có điều kiện
                // Chỉ cập nhật nếu Session vẫn tồn tại, chưa revoke và chưa hết hạn.
                var sessionUpdated = await _dbcontext.UserSessions
                    .Where(x =>
                        x.Id == currentToken.SessionId &&
                        x.UserId == currentToken.UserId &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ExpiresAt, newExpiry)
                        .SetProperty(x => x.LastActivityAt, now),
                        cancellationToken);

                if (sessionUpdated != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = RotateTokenError.SessionInvalid,
                        Error = "Phiên đăng nhập không còn hợp lệ."
                    };
                }

                // 7. Tạo Refresh Token mới
                var http = _httpContextAccessor.HttpContext;

                var refreshToken = _jwtService.GenerateRefreshToken();
                var tokenHash = CommonHelper.Hash(refreshToken);
                var protectedToken = _refreshTokenProtector.Protect(refreshToken);

                var retryUntil = now.AddSeconds(_jwtSettings.RefreshTokenRotationRetrySeconds);

                var newToken = new RefreshToken
                {
                    UserId = currentToken.UserId,
                    SessionId = currentToken.SessionId,
                    TokenHash = tokenHash,
                    CreatedAt = now,
                    ExpiredAt = newExpiry,
                    Lifetime = currentToken.Lifetime,
                    DeviceName = currentToken.DeviceName,
                    UserAgent = http?.Request.Headers.UserAgent.ToString(),
                    IpAddress = CommonHelper.GetClientIp(http),
                    IsRevoked = false
                };

                // 8. Conditional update token cũ
                var affectedRows = await _dbcontext.RefreshTokens
                    .Where(x =>
                        x.Id == currentToken.Id &&
                        !x.IsRevoked &&
                        x.ExpiredAt > now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, "RefreshTokenRotated")
                        .SetProperty(x => x.ReplacedByTokenHash, tokenHash)
                        .SetProperty(x => x.ProtectedReplacedByToken, protectedToken)
                        .SetProperty(x => x.RotationRetryUntil, retryUntil),
                        cancellationToken);

                // 9. Request khác có thể đã rotation token này
                if (affectedRows != 1)
                {
                    var latestToken = await _dbcontext.RefreshTokens
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.Id == currentToken.Id,
                            cancellationToken);

                    if (latestToken == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);

                        return new RotateTokenResult
                        {
                            Success = false,
                            ErrorCode = RotateTokenError.TokenNotFound,
                            Error = "Refresh token không tồn tại."
                        };
                    }

                    if (latestToken.IsRevoked)
                    {
                        if (latestToken.RevokedReason != "RefreshTokenRotated")
                        {
                            await transaction.RollbackAsync(cancellationToken);

                            return new RotateTokenResult
                            {
                                Success = false,
                                ErrorCode = RotateTokenError.TokenRevoked,
                                Error = "Refresh token đã bị thu hồi."
                            };
                        }

                        var retryResult = await GetRetryResultAsync(
                            latestToken,
                            oldToken.User,
                            now,
                            cancellationToken);

                        if (retryResult != null)
                        {
                            if (!retryResult.Success)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return retryResult;
                            }

                            // Request trước đã rotation thành công.
                            // Trả lại cùng replacement token.
                            await transaction.CommitAsync(cancellationToken);

                            return retryResult;
                        }

                        // Retry window đã hết: phát hiện reuse
                        await RevokeSessionForReuseAsync(
                            latestToken.UserId,
                            latestToken.SessionId,
                            now,
                            cancellationToken);

                        await transaction.CommitAsync(cancellationToken);

                        await _logSafetyService.TryLogSecurityAsync(
                            SecurityActionType.RefreshTokenReuse,
                            false,
                            $"Refresh token reuse detected. UserId={latestToken.UserId}, SessionId={latestToken.SessionId}");

                        return new RotateTokenResult
                        {
                            Success = false,
                            ErrorCode = RotateTokenError.ReuseDetected,
                            Error = "Refresh token đã được sử dụng lại."
                        };
                    }

                    await transaction.RollbackAsync(cancellationToken);

                    return new RotateTokenResult
                    {
                        Success = false,
                        ErrorCode = latestToken.ExpiredAt <= now
                            ? RotateTokenError.TokenExpired
                            : RotateTokenError.ConcurrentConflict,
                        Error = "Refresh token không còn hợp lệ hoặc đã xảy ra xung đột."
                    };
                }

                // 10. Lưu Refresh Token mới
                // Session đã được cập nhật ở bước 6 trong cùng transaction.
                _dbcontext.RefreshTokens.Add(newToken);

                await _dbcontext.SaveChangesAsync(cancellationToken);

                // 11. Commit toàn bộ thay đổi
                await transaction.CommitAsync(cancellationToken);

                // Đồng bộ entity đang được tracking
                oldToken.IsRevoked = true;
                oldToken.RevokedAt = now;
                oldToken.RevokedReason = "RefreshTokenRotated";
                oldToken.ReplacedByTokenHash = tokenHash;
                oldToken.ProtectedReplacedByToken = protectedToken;
                oldToken.RotationRetryUntil = retryUntil;

                _logger.LogInformation(
                    "Refresh token rotated successfully. UserId={UserId}, SessionId={SessionId}",
                    currentToken.UserId,
                    currentToken.SessionId);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.RefreshToken,
                    true,
                    $"Refresh token rotated successfully. UserId={currentToken.UserId}");

                return new RotateTokenResult
                {
                    Success = true,
                    ErrorCode = RotateTokenError.None,
                    RefreshToken = refreshToken,
                    RefreshTokenEntity = newToken,
                    User = oldToken.User
                };
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback refresh token transaction. TokenId={TokenId}",
                        oldToken.Id);
                }

                _logger.LogError(
                    ex,
                    "Failed to rotate refresh token. UserId={UserId}, TokenId={TokenId}",
                    oldToken.UserId,
                    oldToken.Id);

                throw;
            }
        }


        public async Task RevokeAsync(
    RefreshToken refreshToken,
    CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var affectedRows = await _dbcontext.RefreshTokens
                .Where(x => x.Id == refreshToken.Id && !x.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, "ManualRevoke"),
                    cancellationToken);

            if (affectedRows == 0)
                return;

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = now;
            refreshToken.RevokedReason = "ManualRevoke";

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.JwtRevoked,
                true,
                $"Refresh token revoked. UserId={refreshToken.UserId}");
        }
        public async Task<int> RevokeAllUserTokensAsync(
     string userId,
     string? reason = null,
     CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            var revokedAt = DateTime.UtcNow;

            return await _dbcontext.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, revokedAt)
                        .SetProperty(x => x.RevokedReason, reason),
                    cancellationToken);
        }

        public async Task<int> RevokeSessionAsync(string userId,
    Guid sessionId,
    string reason,
    string? revokedById = null,
    CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            await using var transaction =
                await _dbcontext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var session = await _dbcontext.UserSessions
                    .FirstOrDefaultAsync(
                        x => x.Id == sessionId && x.UserId == userId,
                        cancellationToken);

                if (session == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return 0;
                }

                if (session.RevokedAt == null)
                {
                    session.RevokedAt = now;
                    session.RevokedReason = reason;
                    session.RevokedById = revokedById;
                }

                var revokedTokens = await _dbcontext.RefreshTokens
                    .Where(x => x.SessionId == sessionId &&
                                x.UserId == userId &&
                                !x.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, reason)
                        .SetProperty(x => x.RevokedById, revokedById),
                        cancellationToken);

                await _dbcontext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Session revoked. UserId={UserId}, SessionId={SessionId}, Reason={Reason}, RevokedTokens={Count}",
                    userId, sessionId, reason, revokedTokens);

                return revokedTokens;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Failed to revoke session. UserId={UserId}, SessionId={SessionId}",
                    userId,
                    sessionId);

                throw;
            }
        }

        public async Task<int> RevokeAllUserSessionsAsync(
    string userId,
    string reason,
    string? revokedById = null,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            await using var transaction =
                await _dbcontext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var revokedTokens = await RevokeAllUserSessionsInTransactionAsync(
                    userId,
                    reason,
                    revokedById,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "All user sessions revoked. UserId={UserId}, Reason={Reason}, RevokedTokens={Count}",
                    userId,
                    reason,
                    revokedTokens);

                return revokedTokens;
            }
            catch (OperationCanceledException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                _logger.LogError(
                    ex,
                    "Failed to revoke all user sessions. UserId={UserId}",
                    userId);

                throw;
            }
        }

        public async Task<int> RevokeAllUserSessionsInTransactionAsync(
            string userId,
            string reason,
            string? revokedById = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            var now = DateTime.UtcNow;

            // 1. Thu hồi toàn bộ session chưa bị thu hồi.
            await _dbcontext.UserSessions
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, reason)
                    .SetProperty(x => x.RevokedById, revokedById),
                    cancellationToken);

            // 2. Thu hồi toàn bộ refresh token chưa bị thu hồi.
            var revokedTokens = await _dbcontext.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, reason)
                    .SetProperty(x => x.RevokedById, revokedById),
                    cancellationToken);

            return revokedTokens;
        }

        private async Task<(string Token, RefreshToken Entity)?> GetRotatedTokenAsync(
     RefreshToken token,
     DateTime now,
     CancellationToken cancellationToken)
        {
            // Chỉ kiểm tra retry window của token ban đầu
            if (!token.RotationRetryUntil.HasValue ||
                token.RotationRetryUntil.Value <= now)
            {
                return null;
            }

            const int maxDepth = 10;

            var current = token;
            var visitedIds = new HashSet<long> { token.Id };

            for (var depth = 0; depth < maxDepth; depth++)
            {
                if (string.IsNullOrWhiteSpace(current.ProtectedReplacedByToken) ||
                    string.IsNullOrWhiteSpace(current.ReplacedByTokenHash))
                {
                    throw new InvalidOperationException(
                        "Refresh token rotation data is incomplete.");
                }

                // Giải mã replacement token
                var replacementToken = _refreshTokenProtector.Unprotect(
                    current.ProtectedReplacedByToken);

                // Kiểm tra hash
                var replacementHash = CommonHelper.Hash(replacementToken);

                if (!string.Equals(
                        replacementHash,
                        current.ReplacedByTokenHash,
                        StringComparison.Ordinal))
                {
                    throw new CryptographicException(
                        "Refresh token replacement hash does not match.");
                }

                // Tìm replacement trong database
                var replacementEntity = await _dbcontext.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.TokenHash == replacementHash,
                        cancellationToken);

                if (replacementEntity == null)
                {
                    throw new InvalidOperationException(
                        "Replacement refresh token was not found.");
                }

                // Kiểm tra quan hệ User và Session
                if (replacementEntity.UserId != token.UserId ||
                    replacementEntity.SessionId != token.SessionId)
                {
                    throw new InvalidOperationException(
                        "Replacement refresh token belongs to another user or session.");
                }

                // Phát hiện chuỗi liên kết vòng
                if (!visitedIds.Add(replacementEntity.Id))
                {
                    throw new InvalidOperationException(
                        "Circular refresh token rotation chain detected.");
                }

                // Replacement vẫn đang hoạt động
                if (!replacementEntity.IsRevoked)
                {
                    if (replacementEntity.ExpiredAt <= now)
                    {
                        throw new InvalidOperationException(
                            "Active replacement refresh token has expired.");
                    }

                    return (replacementToken, replacementEntity);
                }

                // Chỉ tiếp tục nếu replacement cũng đã được rotation
                if (replacementEntity.RevokedReason != "RefreshTokenRotated")
                {
                    throw new InvalidOperationException(
                        "Replacement refresh token was revoked unexpectedly.");
                }

                current = replacementEntity;
            }

            throw new InvalidOperationException(
                "Refresh token rotation chain exceeded maximum depth.");
        }

        private async Task<RotateTokenResult?> GetRetryResultAsync(
    RefreshToken token,
    AppUser user,
    DateTime now,
    CancellationToken cancellationToken)
        {
            var replacement = await GetRotatedTokenAsync(
                token,
                now,
                cancellationToken);

            if (!replacement.HasValue)
                return null;

            var sessionValid = await _dbcontext.UserSessions
                .AnyAsync(
                    x => x.Id == token.SessionId &&
                         x.UserId == token.UserId &&
                         x.RevokedAt == null &&
                         x.ExpiresAt > now,
                    cancellationToken);

            if (!sessionValid)
            {
                return new RotateTokenResult
                {
                    Success = false,
                    ErrorCode = RotateTokenError.SessionInvalid,
                    Error = "Phiên đăng nhập không còn hợp lệ."
                };
            }

            return new RotateTokenResult
            {
                Success = true,
                ErrorCode = RotateTokenError.None,
                RefreshToken = replacement.Value.Token,
                RefreshTokenEntity = replacement.Value.Entity,
                User = user
            };
        }

        private async Task RevokeSessionForReuseAsync(
            string userId,
            Guid sessionId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var session = await _dbcontext.UserSessions
                .FirstOrDefaultAsync(
                    x => x.Id == sessionId && x.UserId == userId,
                    cancellationToken);

            if (session != null && session.RevokedAt == null)
            {
                session.RevokedAt = now;
                session.RevokedReason = "RefreshTokenReuse";
            }

            await _dbcontext.RefreshTokens
                .Where(x => x.SessionId == sessionId &&
                            x.UserId == userId &&
                            !x.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, "SessionRevokedByReuse"),
                    cancellationToken);

            await _dbcontext.SaveChangesAsync(cancellationToken);
        }

        private const string DeviceCookieName = "device_id";

        public string GetOrCreateDeviceId(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(
                DeviceCookieName, out var protectedValue))
            {
                try
                {
                    var deviceId = _deviceProtector.Unprotect(protectedValue);

                    if (Guid.TryParseExact(deviceId, "N", out var parsed) &&
                        parsed != Guid.Empty)
                    {
                        return deviceId;
                    }
                }
                catch (CryptographicException)
                {
                    // Cookie không hợp lệ hoặc không thể giải mã.
                }
            }

            var newDeviceId = Guid.NewGuid().ToString("N");

            context.Response.Cookies.Append(
                DeviceCookieName,
                _deviceProtector.Protect(newDeviceId),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(365),
                    Path = "/"
                });

            return newDeviceId;
        }
    }
}
