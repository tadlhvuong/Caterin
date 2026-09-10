using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Responses;

namespace Shared.Services.Email
{
    public interface IEmailActionService
    {
        Task<string> CreateAsync(string userId, EmailActionType type, string token,
            TimeSpan lifetime, CancellationToken cancellationToken = default);
        Task<ServiceResult<EmailAction>> GetValidAsync(string key, CancellationToken cancellationToken = default);

        Task<ServiceResult> MarkUsedAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ServiceResult> RevokeAsync(string userId, EmailActionType type,
            string? reason = null, CancellationToken cancellationToken = default);
    }
}
