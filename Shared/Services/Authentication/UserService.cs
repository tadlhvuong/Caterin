using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Interfaces.Media;
using Shared.Requests;
using Shared.Requests.User;
using Shared.Responses;
using Shared.Responses.Datatables;
using Shared.Responses.User;
using Shared.Services.Caches;
using System;

namespace Shared.Services.Authentication
{
    public sealed class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMediaService _mediaService;
        private readonly IAppCache _cache;

        private readonly ILogger<UserService> _logger;
        private readonly ISecurityLogger _securityLogger;
        public UserService(AppDbContext dbContext, UserManager<AppUser> userManager, IRefreshTokenService refreshTokenService, RoleManager<AppRole> roleManager,
             ICurrentUserService currentUserService, IMediaService mediaService,
             ILogger<UserService> logger, ISecurityLogger securityLogger, IAppCache cache)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
            _currentUserService = currentUserService;
            _mediaService = mediaService;

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

        public async Task<string> UpdateAvatarAsync(
    string userId,
    IFormFile file,
    CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId && !x.IsDeleted,
                    cancellationToken);

            if (user == null)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            // Lưu lại ID avatar hiện tại để xử lý sau khi cập nhật thành công.
            var oldAvatarId = user.AvatarMediaFileId;

            // Upload file vật lý theo policy Avatar.
            var newMedia = await _mediaService.UploadAsync(
                file,
                "Avatar",
                cancellationToken);

            try
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        cancellationToken);

                // Lưu MediaFile mới.
                _dbContext.MediaFiles.Add(newMedia);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Cập nhật FK avatar của User.
                user.AvatarMediaFileId = newMedia.Id;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            ", ",
                            result.Errors.Select(x => x.Description)));
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Upload mới không được sử dụng nếu cập nhật DB thất bại.
                try
                {
                    await _mediaService.DeleteAsync(newMedia, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to clean up uploaded avatar. UserId: {UserId}",
                        userId);
                }

                throw;
            }

            // Chỉ xử lý avatar cũ sau khi cập nhật thành công.
            if (oldAvatarId.HasValue && oldAvatarId.Value != newMedia.Id)
            {
                await TryDeleteUnusedMediaAsync(
                    oldAvatarId.Value,
                    cancellationToken);
            }

            return $"/uploads/{newMedia.StoragePath}";
        }
        private async Task TryDeleteUnusedMediaAsync(
    long mediaFileId,
    CancellationToken cancellationToken)
        {
            var media = await _dbContext.MediaFiles
                .FirstOrDefaultAsync(
                    x => x.Id == mediaFileId,
                    cancellationToken);

            if (media == null)
                return;

            // Không xóa nếu còn User khác tham chiếu đến file.
            var usedByUser = await _dbContext.Users
                .AnyAsync(
                    x => x.AvatarMediaFileId == mediaFileId,
                    cancellationToken);

            if (usedByUser)
                return;

            // Không xóa file còn được sử dụng bởi Product, Variant hoặc Chat.
            var usedByProduct = await _dbContext.ProductMedias
                .AnyAsync(
                    x => x.MediaFileId == mediaFileId,
                    cancellationToken);

            if (usedByProduct)
                return;

            var usedByVariant = await _dbContext.ProductVariantMedias
                .AnyAsync(
                    x => x.MediaFileId == mediaFileId,
                    cancellationToken);

            if (usedByVariant)
                return;

            var usedByChat = await _dbContext.ChatAttachments
                .AnyAsync(
                    x => x.MediaFileId == mediaFileId,
                    cancellationToken);

            if (usedByChat)
                return;

            // Xóa bản ghi trước để tránh tình trạng DB vẫn tham chiếu
            // tới file vật lý đã bị xóa.
            _dbContext.MediaFiles.Remove(media);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await _mediaService.DeleteAsync(media, cancellationToken);
            }
            catch (Exception ex)
            {
                // File không còn được tham chiếu trong DB.
                // Nếu xóa vật lý thất bại, ghi log để dọn sau.
                _logger.LogWarning(
                    ex,
                    "Failed to delete unused media file. MediaFileId: {MediaFileId}",
                    mediaFileId);
            }
        }

        #region User 

        public async Task<PagedResult<UserListItemResponse>> GetUsersAsync(UserDataTableRequest request)
        {
            var query = _dbContext.Users.Include(x => x.AvatarMediaFile).AsNoTracking().Where(x => !x.IsDeleted);

            // Tổng số tài khoản chưa bị xóa mềm.
            var totalCount = await query.CountAsync();

            // Tìm kiếm.
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    (x.UserName != null && x.UserName.Contains(search)) ||
                    (x.Email != null && x.Email.Contains(search)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));
            }

            // Lọc trạng thái hoạt động.
            if (request.IsActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive == request.IsActive.Value);
            }

            // Lọc trạng thái khóa.
            if (request.IsLocked.HasValue)
            {
                query = query.Where(x => x.IsLocked == request.IsLocked.Value);
            }

            // Lọc theo vai trò.
            if (!string.IsNullOrWhiteSpace(request.UserRole))
            {
                var roleId = request.UserRole.Trim();

                query = query.Where(x =>
                    x.UserRoles.Any(ur => ur.RoleId == roleId));
            }

            if (request.UserStatus.HasValue)
            {
                query = query.Where(x =>
                    (int)x.Status == request.UserStatus.Value);
            }
            var filteredCount = await query.CountAsync();

            // Sắp xếp theo các cột được cho phép.
            var descending = string.Equals(
                request.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            query = request.SortColumn switch
            {
                "UserName" => descending
                    ? query.OrderByDescending(x => x.UserName)
                    : query.OrderBy(x => x.UserName),

                "Email" => descending
                    ? query.OrderByDescending(x => x.Email)
                    : query.OrderBy(x => x.Email),

                "IsActive" => descending
                    ? query.OrderByDescending(x => x.IsActive)
                    : query.OrderBy(x => x.IsActive),

                "IsLocked" => descending
                    ? query.OrderByDescending(x => x.IsLocked)
                    : query.OrderBy(x => x.IsLocked),

                "LastLogin" => descending
                    ? query.OrderByDescending(x => x.LastLogin)
                    : query.OrderBy(x => x.LastLogin),

                _ => descending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt)
            };

            // Phân trang an toàn.
            var start = Math.Max(0, request.Start);
            var length = request.Length <= 0
                ? 10
                : Math.Min(request.Length, 100);
            var now = DateTimeOffset.UtcNow;
            var items = await query
                .Skip(start)
                .Take(length)
                .Select(x => new {
                    User = new UserListItemResponse
                    {
                        Id = x.Id,
                        UserName = x.UserName,
                        Email = x.Email,
                        PhoneNumber = x.PhoneNumber,
                        AvatarUrl = x.AvatarMediaFile != null ? $"/uploads/{x.AvatarMediaFile.StoragePath}" : null,
                        Status = x.Status,
                        IsActive = x.IsActive,
                        IsLocked = x.IsLocked,
                        CreatedAt = x.CreatedAt,
                        LastLogin = x.LastLogin
                    },
                    x.LockoutEnd
                })
                .ToListAsync();
            var resultItems = items.Select(x =>
            {
                x.User.IsIdentityLocked =
                    x.LockoutEnd.HasValue &&
                    x.LockoutEnd.Value > now;

                return x.User;
            }).ToList();
            // Lấy vai trò của các tài khoản trong trang hiện tại.
            // Tránh truy vấn riêng cho từng user (N+1).
            var userIds = resultItems.Select(x => x.Id).ToList();
            if (userIds.Count > 0)
            {
                var userRoles = await _dbContext.UserRoles
                    .AsNoTracking()
                    .Where(x => userIds.Contains(x.UserId))
                    .Select(x => new
                    {
                        x.UserId,
                        RoleName = x.Role.Name
                    })
                    .ToListAsync();

                var rolesByUser = userRoles
                    .GroupBy(x => x.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.RoleName)
                            .Where(name => name != null)
                            .Select(name => name!)
                            .Distinct()
                            .ToList());

                foreach (var item in resultItems)
                {
                    if (rolesByUser.TryGetValue(item.Id, out var roles))
                    {
                        item.Roles = roles;
                    }
                }
            }

            return new PagedResult<UserListItemResponse>
            {
                Items = resultItems,
                Page = start / length + 1,
                PageSize = length,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        public async Task<AppUser> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var userName = request.UserName.Trim();
            var email = request.Email.Trim();

            if (await _userManager.FindByNameAsync(userName) is not null)
                throw new InvalidOperationException(
                    "Tên đăng nhập đã tồn tại.");

            if (await _userManager.FindByEmailAsync(email) is not null)
                throw new InvalidOperationException(
                    "Email đã được sử dụng.");

            const string defaultRole = "User";

            if (!await _roleManager.RoleExistsAsync(defaultRole))
                throw new InvalidOperationException(
                    $"Role '{defaultRole}' không tồn tại.");

            var user = new AppUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim(),
                EmailConfirmed = false,
                IsActive = true,
                IsLocked = false,
                IsDeleted = false,
                PermissionVersion = 1,
                TokenVersion = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedById = _currentUserService.UserId
            };

            var createResult = await _userManager.CreateAsync(
                user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));

                throw new InvalidOperationException($"Không thể tạo tài khoản: {errors}");
            }

            try
            {
                await AssignRoleAsync(user.Id, defaultRole, cancellationToken);
            }
            catch
            {
                // Tránh để lại tài khoản đã tạo nhưng chưa được gán role.
                await _userManager.DeleteAsync(user);
                throw;
            }

            _logger.LogInformation(
                "User account created. UserId: {UserId}",
                user.Id);

            return user;
        }


        public async Task UpdateUserAsync(
     string userId,
     UpdateUserRequest request,
     CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null || user.IsDeleted)
            {
                throw new InvalidOperationException(
                    "Tài khoản không tồn tại.");
            }

            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            var phoneChanged = user.PhoneNumber != phoneNumber;
            var activeChanged = user.IsActive != request.IsActive;

            user.PhoneNumber = phoneNumber;
            user.IsActive = request.IsActive;

            // Tạm thời xác nhận số điện thoại theo quy tắc hiện tại.
            // Khi triển khai xác thực SMS, cần thay đổi logic này.
            if (phoneChanged && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                user.PhoneNumberConfirmed = true;
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = _currentUserService.UserId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Không thể cập nhật tài khoản: {errors}");
            }

            // Vô hiệu hóa các cache liên quan khi trạng thái tài khoản đổi.
            if (activeChanged)
            {
                user.PermissionVersion++;
                user.TokenVersion++;

                var versionResult = await _userManager.UpdateAsync(user);

                if (!versionResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        versionResult.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Không thể cập nhật phiên bản tài khoản: {errors}");
                }
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account updated. UserId: {UserId}",
                userId);
        }

        #endregion User

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
        public async Task ReplaceRolesAsync(
     string userId,
     IEnumerable<string> roles,
     CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            var targetRoles = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in targetRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    throw new InvalidOperationException(
                        $"Vai trò '{role}' không tồn tại.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(
                user, currentRoles);

            if (!removeResult.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", removeResult.Errors.Select(e => e.Description)));

            if (targetRoles.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(
                    user, targetRoles);

                if (!addResult.Succeeded)
                {
                    // Không nên để tài khoản mất vai trò nếu thao tác thêm thất bại.
                    // Cần xử lý rollback/khôi phục vai trò cũ tại đây.
                    throw new InvalidOperationException(
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                }
            }

            await IncreasePermissionVersionInternalAsync(
                user, SecurityActionType.RoleReplaced);

            await InvalidateUserCache(userId);
        }

        public async Task<int?> GetUserLevelAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await (
                from ur in _dbContext.UserRoles.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking()
                    on ur.RoleId equals role.Id
                where ur.UserId == userId
                select (int?)role.Level
            ).MinAsync(cancellationToken);
        }

        #endregion Role


        public async Task ResetPasswordAsync(
            string userId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("Tài khoản không tồn tại.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user, token, newPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Đổi mật khẩu lỗi: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }
            user.UpdatedById = _currentUserService.UserId;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Cập nhật thông tin tài khoản lỗi: {string.Join(", ", updateResult.Errors.Select(x => x.Description))}");
            }
            // Vô hiệu hóa access token cũ thông qua PermissionVersion.
            await IncreasePermissionVersionInternalAsync(
                user, SecurityActionType.ResetPassword);

            // Cập nhật SecurityStamp để validator từ chối JWT cũ.
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);

            if (!stampResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Cập nhật SecurityStamp lỗi: {string.Join(", ", stampResult.Errors.Select(x => x.Description))}");
            }

            // Thu hồi refresh token trên tất cả thiết bị.
            await _refreshTokenService.RevokeAllUserTokensAsync(
                userId, cancellationToken);

            // Xóa cache liên quan.
            await InvalidateUserCache(userId);
        }

        #region User Status
        public async Task LockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            user.UpdatedById = _currentUserService.UserId;
            user.UpdatedAt = DateTime.UtcNow;
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

            await InvalidateUserCache(userId);
            await _securityLogger.LogAsync(SecurityActionType.Lock, true, $"Khóa tài khoản {user.Email}");
        }
        public async Task UnlockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;


            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedById = _currentUserService.UserId;
            user.UpdatedAt = DateTime.UtcNow;
            user.PermissionVersion++;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));
            await InvalidateUserCache(userId);
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
            user.UpdatedById = _currentUserService.UserId;
            user.UpdatedAt = DateTime.UtcNow;
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));
            await InvalidateUserCache(userId);
            await _securityLogger.LogAsync(SecurityActionType.Disable, true, $"Tăng version quyền hạn khi vô hiệu hóa tài khoản: {user.Id} ");
        }
        public async Task EnableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.IsActive = true;
            user.UpdatedById = _currentUserService.UserId;
            user.UpdatedAt = DateTime.UtcNow;
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));
            await InvalidateUserCache(userId);
            await _securityLogger.LogAsync(SecurityActionType.Enable, true, $"Tăng version quyền hạn khi mở hoạt động tài khoản: {user.Id} ");
        }

        public async Task<ServiceResult> DeactivateAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return ServiceResult.Fail("Tài khoản không tồn tại.");

            if (!user.IsActive)
                return ServiceResult.Fail("Tài khoản đã bị vô hiệu hóa.");
            user.IsDeleted = true;
            user.Status = EntityStatus.Deleted;
            user.PermissionVersion++;
            user.TokenVersion++;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description));

                return ServiceResult.Fail($"Không thể vô hiệu hóa tài khoản: {errors}");
            }

            return ServiceResult.Success("Đã vô hiệu hóa tài khoản thành công.");
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

    }
}
