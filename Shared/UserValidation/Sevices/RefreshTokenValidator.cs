using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;

namespace Shared.UserValidation.Sevices
{
    public sealed class RefreshTokenValidator : IRefreshTokenValidator
    {

        public async Task<UserValidationResult> ValidateAsync(UserValidationContext context,
    AppUser user,
    CancellationToken cancellationToken = default)
        {
            if (context.Scenario != UserValidationScenario.RefreshToken)
                return UserValidationResult.Success();

            var refreshToken = context.RefreshTokenEntity;

            if (refreshToken is null)
                return UserValidationResult.Fail(
                    UserValidationError.RefreshTokenNotFound);

            if (refreshToken.UserId != user.Id)
                return UserValidationResult.Fail(
                    UserValidationError.InvalidRefreshToken);

            if (refreshToken.IsRevoked)
                return UserValidationResult.Fail(
                    UserValidationError.RefreshTokenRevoked);

            if (refreshToken.ExpiredAt <= DateTime.UtcNow)
                return UserValidationResult.Fail(
                    UserValidationError.RefreshTokenExpired);

            // Kiểm tra Session thuộc đúng User và còn hiệu lực
            if (refreshToken.Session == null ||
                refreshToken.Session.UserId != user.Id ||
                refreshToken.Session.RevokedAt != null ||
                refreshToken.Session.ExpiresAt <= DateTime.UtcNow)
            {
                return UserValidationResult.Fail(
                    UserValidationError.SessionInvalid);
            }

            return UserValidationResult.Success();
        }

    }
}
