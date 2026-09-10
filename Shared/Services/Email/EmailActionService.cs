using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Responses;

namespace Shared.Services.Email
{
    using Microsoft.EntityFrameworkCore;

    public sealed class EmailActionService : IEmailActionService
    {
        private readonly AppDbContext _dbcontext;

        public EmailActionService(AppDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<string> CreateAsync(string userId, EmailActionType type, string token,
            TimeSpan lifetime, CancellationToken cancellationToken = default)
        {
            var key = CommonHelper.GenerateSecureToken();

            var entity = new EmailAction
            {
                Id = Guid.NewGuid(),
                KeyHash = CommonHelper.Hash(key),
                UserId = userId,
                Type = type,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.Add(lifetime)
            };

            _dbcontext.EmailActions.Add(entity);

            await _dbcontext.SaveChangesAsync(cancellationToken);

            return key;
        }

        public async Task<ServiceResult<EmailAction>> GetValidAsync(string key, CancellationToken cancellationToken = default)
        {
            var hash = CommonHelper.Hash(key);

            var entity = await _dbcontext.EmailActions.SingleOrDefaultAsync(x => x.KeyHash == hash, cancellationToken);

            if (entity == null)
                return ServiceResult<EmailAction>.Fail("Liên kết không tồn tại.");

            if (entity.RevokedAt != null)
                return ServiceResult<EmailAction>.Fail("Liên kết đã bị thu hồi.");

            if (entity.UsedAt != null)
                return ServiceResult<EmailAction>.Fail("Liên kết đã được sử dụng.");

            if (entity.ExpiredAt <= DateTime.UtcNow)
                return ServiceResult<EmailAction>.Fail("Liên kết đã hết hạn.");

            return ServiceResult<EmailAction>.Success(entity);
        }

        public async Task<ServiceResult> MarkUsedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbcontext.EmailActions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return ServiceResult.Fail("Dữ liệu email đã gửi không còn tồn tại.");

            if (entity.UsedAt != null)
                return ServiceResult.Success();

            entity.UsedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RevokeAsync(string userId, EmailActionType type,
            string? reason = null, CancellationToken cancellationToken = default)
        {
            var actions = await _dbcontext.EmailActions
                .Where(x => x.UserId == userId && x.Type == type && x.UsedAt == null && x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            if (actions.Count == 0)
                return ServiceResult.Success();

            foreach (var item in actions)
            {
                item.RevokedAt = DateTime.UtcNow;
                item.RevokedReason = reason;
            }

            await _dbcontext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success();
        }
    }
}
