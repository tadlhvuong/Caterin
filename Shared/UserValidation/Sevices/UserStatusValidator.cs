using Microsoft.AspNetCore.Identity;
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
    public sealed class UserStatusValidator : IUserStatusValidator
    {
        private readonly UserManager<AppUser> _userManager;

        public UserStatusValidator(
            UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserValidationResult> ValidateAsync(
    UserValidationContext context,
    AppUser user,
    CancellationToken cancellationToken = default)
        {
            if (user is null)
            {
                return UserValidationResult.Fail(
                    UserValidationError.UserNotFound);
            }

            if (user.IsDeleted)
            {
                return UserValidationResult.Fail(
                    UserValidationError.UserDeleted);
            }

            if (!user.IsActive)
            {
                return UserValidationResult.Fail(
                    UserValidationError.UserInactive);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return UserValidationResult.Fail(
                    UserValidationError.UserLockedOut);
            }

            if (context.Scenario == UserValidationScenario.Login)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    return UserValidationResult.Fail(
                        UserValidationError.EmailNotConfirmed);
                }

                // Nếu hệ thống bắt buộc xác thực số điện thoại
                /*
                if (!await _userManager.IsPhoneNumberConfirmedAsync(user))
                {
                    return UserValidationResult.Fail(
                        UserValidationError.PhoneNotConfirmed);
                }
                */
            }

            return UserValidationResult.Success();
        }
    }   
}
