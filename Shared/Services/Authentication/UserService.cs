using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Responses;
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
        public UserService(AppDbContext dbContext, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager,
            ILogger<UserService> logger, ISecurityLogger securityLogger, IAppCache cache)
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
        
        #region Role
        public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Tài khoản không tồn tại.");

            role = role.Trim();
            if (!await _roleManager.RoleExistsAsync(role))
                throw new Exception($"Role '{role}' không tồn tại.");

            if (await _userManager.IsInRoleAsync(user, role))
                return;

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleAssigned);

            await _securityLogger.LogAsync(SecurityActionType.RoleAssigned, true, $"Role {role}: thêm cho tài khoản {userId}");

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
            await _securityLogger.LogAsync(SecurityActionType.RoleRemoved, true, $"Xóa role {role}: từ tài khoản {userId}");
        }
        public async Task ReplaceRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Tài khoản không tồn tại.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            //var targetMaxLevel = await GetMaxRoleLevel(roles);

            //if (currentRoles.Level < targetMaxLevel)
            //    throw new UnauthorizedAccessException();
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                throw new Exception("Xóa role không thành công.");

            roles = roles.Select(r => r.Trim()).Distinct();
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    throw new Exception($"Role '{role}': không tồn tại.");

                var addResult = await _userManager.AddToRolesAsync(user, roles);
            }

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleReplaced);
            await InvalidateUserCache(userId);
        }
        #endregion Role

        public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("Tài khoản không tồn tại.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                throw new Exception($"Đổi mật khẩu lỗi: {string.Join(", ", result.Errors.Select(x => x.Description))}");

            await IncreasePermissionVersionInternalAsync(user, SecurityActionType.ResetPassword);
            await _userManager.UpdateSecurityStampAsync(user);

            await InvalidateUserCache(userId);
        }

        #region User Status
        public async Task LockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.Lock, true, $"Khóa tài khoản {user.Email}");
        }
        public async Task UnlockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;


            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.PermissionVersion++;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await _securityLogger.LogAsync(SecurityActionType.UnLock, true, $"Mở tài khoản {user.Email}");
        }
        public Task<bool> IsLockedAsync(AppUser user)
        {
            return _userManager.IsLockedOutAsync(user);
        }
        public async Task<bool> IsLockedIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            return await _userManager.IsLockedOutAsync(user);
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
            await _securityLogger.LogAsync(SecurityActionType.Disable, true, $"Tăng version quyền hạn khi vô hiệu hóa tài khoản: {user.Id} ");
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

            await _securityLogger.LogAsync(SecurityActionType.Enable, true, $"Tăng version quyền hạn khi mở hoạt động tài khoản: {user.Id} ");
        }
        #endregion User Status

        #region Permission
        public async Task IncreasePermissionVersionAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

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

            await _securityLogger.LogAsync(action, true, $"Tăng version quyền hạn cho tài khoản: {user.Id} ");
        }
        private async Task InvalidateUserCache(string userId)
        {
            await _cache.RemoveAsync(CacheKeys.UserPermission(userId));
            await _cache.RemoveAsync(CacheKeys.UserRoles(userId));
        }
        #endregion Permission

        public async Task<PagedResult<UserListResponse>> GetUsersAsync(UserQueryRequest request, CancellationToken cancellationToken = default)
        {
            var query = _userManager.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.Email!.ToLower().Contains(keyword) ||
                    x.UserName!.ToLower().Contains(keyword));
            }

            if (request.EmailConfirmed.HasValue)
            {
                query = query.Where(x => x.EmailConfirmed == request.EmailConfirmed.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == (EntityStatus)request.Status.Value);
            }

            if (request.Locked.HasValue)
            {
                if (request.Locked.Value)
                {
                    query = query.Where(x =>
                        x.LockoutEnd != null &&
                        x.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else
                {
                    query = query.Where(x =>
                        x.LockoutEnd == null ||
                        x.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            // Filter Role
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                query = query.Where(x =>
                    x.UserRoles.Any(r => r.Role.Name == request.Role));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new UserListResponse
                {
                    Id = x.Id,
                    Avatar = x.Avatar,
                    FullName = x.UserName,
                    Email = x.Email!,
                    Billing = "Cash",
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var userIds = users.Select(x => x.Id).ToList();

            var roles = await (
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new
                {
                    ur.UserId,
                    r.Name
                })
                .ToListAsync(cancellationToken);

            var roleLookup = roles
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Name!).ToList());

            foreach (var user in users)
            {
                user.Roles = roleLookup.GetValueOrDefault(user.Id) ?? [];
            }

            return new PagedResult<UserListResponse>
            {
                Items = users,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
