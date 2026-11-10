using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Responses;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Shared.Constants.Core;

namespace Shared.Services.Email
{

    public sealed class EmailActionService : IEmailActionService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IDataProtector _tokenProtector;

        public EmailActionService(
            AppDbContext context,
            IDataProtectionProvider dataProtectionProvider)
        {
            _dbcontext = context;

            _tokenProtector = dataProtectionProvider.CreateProtector(DataProtectionPurposes.EmailActionToken);
        }

        public async Task<string> CreateAsync(
            string userId,
            EmailActionType type,
            string token,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token is required.", nameof(token));

            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifetime));

            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;
            var key = CommonHelper.GenerateSecureToken();

            var entity = new EmailAction
            {
                Id = Guid.NewGuid(),
                KeyHash = CommonHelper.Hash(key),
                UserId = userId,
                Type = type,

                // Bảo vệ token trước khi lưu database.
                Token = _tokenProtector.Protect(token),

                CreatedAt = now,
                ExpiredAt = now.Add(lifetime)
            };

            _dbcontext.EmailActions.Add(entity);

            await _dbcontext.SaveChangesAsync(cancellationToken);

            return key;
        }
        public async Task<string> CreateReplacingAsync(
    string userId,
    EmailActionType type,
    string token,
    TimeSpan lifetime,
    string? reason = null,
    CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifetime));

            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _dbcontext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;

                // Thu hồi các link cũ chưa sử dụng.
                await _dbcontext.EmailActions
                    .Where(x =>
                        x.UserId == userId &&
                        x.Type == type &&
                        x.UsedAt == null &&
                        x.RevokedAt == null)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.RevokedAt, now)
                            .SetProperty(x => x.RevokedReason, reason),
                        cancellationToken);

                // Tạo link mới.
                var key = CommonHelper.GenerateSecureToken();

                var entity = new EmailAction
                {
                    Id = Guid.NewGuid(),
                    KeyHash = CommonHelper.Hash(key),
                    UserId = userId,
                    Type = type,
                    Token = _tokenProtector.Protect(token),
                    CreatedAt = now,
                    ExpiredAt = now.Add(lifetime)
                };

                _dbcontext.EmailActions.Add(entity);

                await _dbcontext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return key;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        public async Task<ServiceResult<EmailAction>> GetValidAsync(
    string key,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return ServiceResult<EmailAction>.Fail("Liên kết không hợp lệ.");

            var keyHash = CommonHelper.Hash(key);
            var now = DateTime.UtcNow;

            var action = await _dbcontext.EmailActions
                .FirstOrDefaultAsync(x =>
                    x.KeyHash == keyHash &&
                    x.UsedAt == null &&
                    x.RevokedAt == null &&
                    x.ExpiredAt > now,
                    cancellationToken);

            if (action == null)
                return ServiceResult<EmailAction>.Fail(
                    "Liên kết không hợp lệ hoặc đã hết hạn.");

            if (!TryUnprotectToken(action.Token, out var token))
                return ServiceResult<EmailAction>.Fail(
                    "Liên kết không hợp lệ hoặc đã hết hạn.");

            action.Token = token;

            return ServiceResult<EmailAction>.Success(action);
        }
        public async Task<ServiceResult<EmailAction>> GetValidAsync(
    string key,
    EmailActionType type,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return ServiceResult<EmailAction>.Fail("Liên kết không hợp lệ.");

            var keyHash = CommonHelper.Hash(key);
            var now = DateTime.UtcNow;

            var action = await _dbcontext.EmailActions
                .FirstOrDefaultAsync(x =>
                    x.KeyHash == keyHash &&
                    x.Type == type &&
                    x.UsedAt == null &&
                    x.RevokedAt == null &&
                    x.ExpiredAt > now,
                    cancellationToken);

            if (action == null)
                return ServiceResult<EmailAction>.Fail(
                    "Liên kết không hợp lệ hoặc đã hết hạn.");

            if (!TryUnprotectToken(action.Token, out var token))
                return ServiceResult<EmailAction>.Fail(
                    "Liên kết không hợp lệ hoặc đã hết hạn.");

            action.Token = token;

            return ServiceResult<EmailAction>.Success(action);
        }

        public async Task<ServiceResult> MarkUsedAsync(
            Guid id,
            string userId,
            EmailActionType type,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Fail("Thông tin liên kết không hợp lệ.");

            var now = DateTime.UtcNow;

            var affectedRows = await _dbcontext.EmailActions
                .Where(x =>
                    x.Id == id &&
                    x.UserId == userId &&
                    x.Type == type &&
                    x.UsedAt == null &&
                    x.RevokedAt == null &&
                    x.ExpiredAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.UsedAt,
                        now),
                    cancellationToken);

            if (affectedRows == 0)
            {
                return ServiceResult.Fail(
                    "Liên kết không hợp lệ, đã được sử dụng hoặc hết hạn.");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RevokeAsync(
            string userId,
            EmailActionType type,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Fail("User không hợp lệ.");

            var now = DateTime.UtcNow;

            await _dbcontext.EmailActions
                .Where(x =>
                    x.UserId == userId &&
                    x.Type == type &&
                    x.UsedAt == null &&
                    x.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, reason),
                    cancellationToken);

            return ServiceResult.Success();
        }

        private bool TryUnprotectToken(
    string protectedToken,
    out string token)
        {
            token = string.Empty;

            try
            {
                token = _tokenProtector.Unprotect(protectedToken);
                return !string.IsNullOrWhiteSpace(token);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
