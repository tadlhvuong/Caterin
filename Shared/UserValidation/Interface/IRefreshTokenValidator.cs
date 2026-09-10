using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;

namespace Shared.UserValidation.Interface
{
    public interface IRefreshTokenValidator
    {
        Task<UserValidationResult> ValidateAsync(UserValidationContext context, AppUser user, CancellationToken cancellationToken = default);
    }
}
