using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Sevices
{
    public sealed class RefreshTokenValidator : IRefreshTokenValidator
    {
        public Task<UserValidationResult> ValidateAsync(
            UserValidationContext context,
            AppUser user,
            CancellationToken cancellationToken = default)
        {
            if (context.Scenario != UserValidationScenario.RefreshToken)
            {
                return Task.FromResult(UserValidationResult.Success());
            }

            var refreshToken = context.RefreshTokenEntity;

            if (refreshToken is null)
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.RefreshTokenNotFound));
            }

            if (refreshToken.UserId != user.Id)
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.InvalidRefreshToken));
            }

            if (refreshToken.IsRevoked)
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.RefreshTokenRevoked));
            }

            if (refreshToken.ExpiredAt <= DateTime.UtcNow)
            {
                return Task.FromResult(
                    UserValidationResult.Fail(UserValidationError.RefreshTokenExpired));
            }

            return Task.FromResult(UserValidationResult.Success());
        }
    }
}
