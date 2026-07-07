using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.Log;
using Shared.Services.Caches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Shared.Services.Authentication
{
    public sealed class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ILogger<UserService> _logger;
        private readonly ISecurityLogger _securityLogger;
        private readonly IAppCache _cache;

        public UserService(
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            ILogger<UserService> logger,
            ISecurityLogger securityLogger,
            IAppCache cache)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _securityLogger = securityLogger;
            _cache = cache;
        }
        public async Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByIdAsync(userId);
        }
        public async Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByNameAsync(userName);
        }
        public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByEmailAsync(email);
        }
        public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Array.Empty<string>();

            var roles = await _userManager.GetRolesAsync(user);

            return roles.ToList();
        }
        public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            role = role.Trim();
            if (!await _roleManager.RoleExistsAsync(role))
                throw new Exception($"Role '{role}' not found.");

            if (await _userManager.IsInRoleAsync(user, role))
                return;
            //var currentUser = await _userService.GetCurrentUserAsync();
            //var currentRoles = await _userManager.GetRolesAsync(currentUser);

            //var currentUserRole = await _roleService.GetHighestRoleAsync(currentRoles);
            //var targetRole = await _roleManager.FindByNameAsync(role);

            //if (currentUserRole.Level < targetRole.Level)
            //    throw new UnauthorizedAccessException();
            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleAssigned);

            await _securityLogger.LogAsync(SecurityActionType.RoleAssigned,
                true,
                $"Assigned role {role} to user {userId}");

            await InvalidateUserCache(userId);
        }
        public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (!await _userManager.IsInRoleAsync(user, role))
                return;

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleRemoved);
            await InvalidateUserCache(userId);
            await _securityLogger.LogAsync(SecurityActionType.RoleRemoved, true,
                $"Removed role {role} from user {userId}");
        }
        public async Task ReplaceRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            //var targetMaxLevel = await GetMaxRoleLevel(roles);

            //if (currentRoles.Level < targetMaxLevel)
            //    throw new UnauthorizedAccessException();
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                throw new Exception("Failed to remove roles.");

            roles = roles.Select(r => r.Trim()).Distinct();
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    throw new Exception($"Role '{role}' not found.");

                var addResult = await _userManager.AddToRolesAsync(user, roles);
            }

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleReplaced);
            await InvalidateUserCache(userId);
        }
        public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.ResetPassword);
            await _userManager.UpdateSecurityStampAsync(user);

            await InvalidateUserCache(userId);
        }
        public async Task LockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.Lock, true,
                $"PermissionVersion increased for user {user.Id} lock");
        }
        public async Task UnlockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = null;
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.UnLock, true,
                $"PermissionVersion increased for user {user.Id} UnLock");
        }
        public async Task DisableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.IsActive = false;
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.Disable, true,
                $"PermissionVersion increased for user {user.Id} Disabled");
        }
        public async Task EnableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.IsActive = true;

            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.Enable, true,
                $"PermissionVersion increased for user {user.Id} Enabled");
        }

        public async Task IncreasePermissionVersionAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.PermissionChanged);
        }
        public async Task IncreasePermissionVersionByRoleAsync(string role, CancellationToken cancellationToken = default)
        {
            var roleId = await _dbContext.Roles.Where(r => r.Name == role).
                Select(r => r.Id).FirstOrDefaultAsync();

            if (roleId == null)
                return;

            var userIds = await _dbContext.UserRoles.Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId).ToListAsync(cancellationToken);

            await _dbContext.Users.Where(u => userIds.Contains(u.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PermissionVersion, u => u.PermissionVersion + 1), cancellationToken);
            await _securityLogger.LogAsync(SecurityActionType.RoleBulkUpdated, true,
                $"PermissionVersion increased for role {role}");
            foreach (var userId in userIds)
            {
                await InvalidateUserCache(userId);
            }
        }

        private async Task IncreasePermissionVersionInternalAsync(AppUser user, SecurityActionType action)
        {
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(action,true,
                $"PermissionVersion increased for user {user.Id}");
        }
        private async Task InvalidateUserCache(string userId)
        {
            await _cache.RemoveAsync(CacheKeys.UserPermission(userId));
            await _cache.RemoveAsync(CacheKeys.UserRoles(userId));
        }
    }
}
