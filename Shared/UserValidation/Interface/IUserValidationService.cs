using Shared.UserValidation.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Interface
{
    public interface IUserValidationService
    {
        Task<UserValidationResult> ValidateAsync(
            UserValidationContext context,
            CancellationToken cancellationToken = default);


        Task<bool> HasPermissionAsync(UserValidationContext context, string permissionCode);
        bool IsRootUserAsync(UserValidationContext context);
        Task<bool> HasAdminAccessAsync(UserValidationContext context);
    }
}
