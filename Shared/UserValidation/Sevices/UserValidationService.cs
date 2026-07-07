using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants.Permission;
using Shared.Data.Entities.Identity;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Services.Authentication;
using Shared.Services.Caches;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;

namespace Shared.UserValidation.Sevices
{
    public sealed class UserValidationService : IUserValidationService
    {
        private readonly UserManager<AppUser> _userManager;

        private readonly Interface.IUserStatusValidator _userStatusValidator;
        private readonly Interface.ISecurityStampValidator _securityStampValidator;
        private readonly IRefreshTokenValidator _refreshTokenValidator;
        private readonly IPermissionVersionValidator _permissionVersionValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserValidationService> _logger;
        private readonly IAppCache _cache;

        public UserValidationService(
            UserManager<AppUser> userManager,
            IUserStatusValidator userStatusValidator,
            Interface.ISecurityStampValidator securityStampValidator,
            IRefreshTokenValidator refreshTokenValidator,
            IPermissionVersionValidator permissionVersionValidator,
            IPermissionService permissionService, ILogger<UserValidationService> logger,
            IAppCache cache)
        {
            _userManager = userManager;

            _userStatusValidator = userStatusValidator;
            _securityStampValidator = securityStampValidator;
            _refreshTokenValidator = refreshTokenValidator;
            _permissionVersionValidator = permissionVersionValidator;
            _permissionService = permissionService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<UserValidationResult> ValidateAsync(UserValidationContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var user = await LoadUserAsync(context, cancellationToken);

            if (user is null)
            {
                return UserValidationResult.Fail(UserValidationError.UserNotFound);
            }

            UserValidationResult result;

            //------------------------------------
            // User Status
            //------------------------------------

            result = await _userStatusValidator.ValidateAsync(context, user, cancellationToken);

            if (!result.Succeeded)
                return result;

            //------------------------------------
            // Security Stamp
            //------------------------------------

            result = await _securityStampValidator.ValidateAsync(context, user, cancellationToken);

            if (!result.Succeeded)
                return result;

            //------------------------------------
            // Refresh Token
            //------------------------------------

            result = await _refreshTokenValidator.ValidateAsync(context, user, cancellationToken);

            if (!result.Succeeded)
                return result;

            //------------------------------------
            // Permission Version
            //------------------------------------

            result = await _permissionVersionValidator.ValidateAsync(context, user, cancellationToken);

            if (!result.Succeeded)
                return result;

            //------------------------------------
            context.PermissionSnapshot = await _permissionService.
                GetUserPermissionSnapshotAsync(user.Id, user.PermissionVersion, cancellationToken);

            return UserValidationResult.Success();
        }

        private async Task<AppUser?> LoadUserAsync(UserValidationContext context, CancellationToken cancellationToken)
        {
            switch (context.Scenario)
            {
                //--------------------------------
                // Login
                //--------------------------------

                case UserValidationScenario.Login:

                    return await _userManager.FindByIdAsync(
                        context.UserId!);

                //--------------------------------
                // Access Token
                //--------------------------------

                case UserValidationScenario.AccessToken:

                    var id = _userManager.GetUserId(
                        context.Principal!);

                    if (string.IsNullOrWhiteSpace(id))
                        return null;

                    return await _userManager.FindByIdAsync(id);

                //--------------------------------
                // Refresh Token
                //--------------------------------

                case UserValidationScenario.RefreshToken:

                    return await _userManager.FindByIdAsync(
                        context.UserId!);

                default:

                    return null;
            }
        }
        #region Permission Check

        public async Task<bool> HasPermissionAsync(UserValidationContext context, string permissionCode)
        {
            var permissionId = await _permissionService.GetPermissionIdAsync(permissionCode);

            if (permissionId == null)
            {
                _logger.LogWarning("Permission code not found: {PermissionCode}", permissionCode);
                return false;
            }

            var snapshot = context.PermissionSnapshot;

            if (snapshot == null)
                return false;

            if (snapshot.IsRoot)
                return true;

            return snapshot.PermissionIds.Contains(permissionId.Value);
        }

        #endregion

        #region Root User Check

        public bool IsRootUserAsync(UserValidationContext context)
        {
            return context.PermissionSnapshot?.IsRoot ?? false;
        }

        #endregion

        #region Admin Access Check

        public async Task<bool> HasAdminAccessAsync(UserValidationContext context)
        {
            if (context.PermissionSnapshot?.IsRoot == true)
                return true;

            var permissionId =
                await _permissionService.GetPermissionIdAsync(PermissionCodes.AdminAccess);

            if (permissionId == null)
                return false;

            return context.PermissionSnapshot!.PermissionIds.Contains(permissionId.Value);
        }

        #endregion

    }
}
