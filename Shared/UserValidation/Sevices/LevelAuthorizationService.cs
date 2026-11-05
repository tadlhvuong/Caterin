using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Interfaces.IdentityServices;
using Shared.UserValidation.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Sevices
{
    public class LevelAuthorizationService : ILevelAuthorizationService
    {
        private readonly AppDbContext _dbContext;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ICurrentUserService _currentUserService;

        public LevelAuthorizationService(
            AppDbContext dbContext, RoleManager<AppRole> roleManager,
        ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _currentUserService = currentUserService;
        }
        public async Task<int?> GetUserLevelAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await (
                from userRole in _dbContext.UserRoles.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where userRole.UserId == userId
                select (int?)role.Level
            ).MinAsync(cancellationToken);
        }

        public async Task EnsureCanManageRoleAsync(
    string roleName,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException(
                    "Tên vai trò không hợp lệ.",
                    nameof(roleName));

            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new UnauthorizedAccessException(
                    "Không xác định được người thao tác.");

            var currentLevel = await GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            if (!currentLevel.HasValue)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền thực hiện thao tác này.");

            var role = await _roleManager.FindByNameAsync(roleName.Trim());

            if (role == null)
                throw new KeyNotFoundException(
                    $"Vai trò '{roleName}' không tồn tại.");

            if (currentLevel.Value >= role.Level)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền quản lý vai trò này.");
        }

        public async Task EnsureCanManageUserAsync(
    string targetUserId,
    bool allowSelf = false,
    CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new UnauthorizedAccessException(
                    "Không xác định được người thao tác.");

            // Chính sách cho phép tự thao tác.
            if (currentUserId == targetUserId)
            {
                if (allowSelf)
                    return;

                throw new UnauthorizedAccessException(
                    "Bạn không được phép thực hiện thao tác này trên chính tài khoản của mình.");
            }

            var currentLevel = await GetUserLevelAsync(
                currentUserId, cancellationToken);

            var targetLevel = await GetUserLevelAsync(
                targetUserId, cancellationToken);

            if (!currentLevel.HasValue ||
                currentLevel.Value >= (targetLevel ?? int.MaxValue))
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền quản lý tài khoản này.");
            }
        }

        public async Task EnsureCanCreateRoleAsync(
    int newRoleLevel,
    CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new UnauthorizedAccessException(
                    "Không xác định được người thao tác.");

            var currentLevel = await GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            if (!currentLevel.HasValue ||
                currentLevel.Value >= newRoleLevel)
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền tạo vai trò ở cấp bậc này.");
            }
        }

        public async Task EnsureCanManageRoleLevelAsync(
    int targetRoleLevel,
    CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new UnauthorizedAccessException(
                    "Không xác định được người thao tác.");

            var currentLevel = await GetUserLevelAsync(
                currentUserId, cancellationToken);

            if (!currentLevel.HasValue)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền thực hiện thao tác này.");

            if (currentLevel.Value >= targetRoleLevel)
                throw new UnauthorizedAccessException(
                    "Bạn không được phép thiết lập Level này.");
        }
    }
}
