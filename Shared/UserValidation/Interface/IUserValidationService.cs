using Shared.UserValidation.DTOs;

namespace Shared.UserValidation.Interface
{
    public interface IUserValidationService
    {
        Task<UserValidationResult> ValidateAsync(UserValidationContext context, CancellationToken cancellationToken = default);

        Task<bool> HasPermissionAsync(UserValidationContext context, string permissionCode);

        bool IsRootUserAsync(UserValidationContext context);

        Task<bool> HasAdminAccessAsync(UserValidationContext context);
    }
}
