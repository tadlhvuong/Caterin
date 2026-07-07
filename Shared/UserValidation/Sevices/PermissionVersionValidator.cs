    using Shared.Constants.Permission;
using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Sevices
{
    public sealed class PermissionVersionValidator : IPermissionVersionValidator
    {
        public Task<UserValidationResult> ValidateAsync(
            UserValidationContext context,
            AppUser user,
            CancellationToken cancellationToken = default)
        {
            if (context.Scenario != UserValidationScenario.AccessToken)
                return Task.FromResult(UserValidationResult.Success());

            var principal = context.Principal;

            if (principal == null)
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.PrincipalMissed));

            var claimValue = principal.FindFirstValue(ClaimConstants.PermissionVersion);

            if (string.IsNullOrWhiteSpace(claimValue))
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.PermissionVersionMissed));

            if (!long.TryParse(claimValue, out var jwtVersion))
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.PermissionVersionInvalid));

            if (jwtVersion != user.PermissionVersion)
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.PermissionVersionChanged));

            return Task.FromResult(UserValidationResult.Success());
        }
    }
}
