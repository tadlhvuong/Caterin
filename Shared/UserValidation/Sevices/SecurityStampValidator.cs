using Shared.Constants.Permission;
using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Sevices
{
    public sealed class SecurityStampValidator : ISecurityStampValidator
    {
        public Task<UserValidationResult> ValidateAsync(
            UserValidationContext context,
            AppUser user,
            CancellationToken cancellationToken = default)
        {
            if (context.Scenario is UserValidationScenario.Login or UserValidationScenario.RefreshToken)
                return Task.FromResult(UserValidationResult.Success());

            if (context.Principal == null)
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.PrincipalMissed));

            var tokenStamp = context.Principal
            .FindFirstValue(ClaimConstants.SecurityStamp);

            if (string.IsNullOrWhiteSpace(tokenStamp))
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.SecurityStampMissed));
            }

            if (!string.Equals(tokenStamp, user.SecurityStamp, StringComparison.Ordinal))
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.SecurityStampChanged));
            }

            return Task.FromResult(UserValidationResult.Success());
        }
    }
}
