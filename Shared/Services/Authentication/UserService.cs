using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.Identity;
using Shared.DTOs.User;
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
        private readonly IActivityLogger _activityLogger;
        public UserService(AppDbContext dbContext, UserManager<AppUser> userManager, IRefreshTokenService refreshTokenService, RoleManager<AppRole> roleManager,
             ICurrentUserService currentUserService, IMediaService mediaService,
             ILogger<UserService> logger, ISecurityLogger securityLogger, IActivityLogger activityLogger,
             IAppCache cache)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
            _currentUserService = currentUserService;
            _mediaService = mediaService;

            _logger = logger;
            _securityLogger = securityLogger;
            _activityLogger = activityLogger;
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
        #region USER 

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
        
        public async Task<UserDetailsDto?> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .Include(x => x.AvatarMediaFile)
                    .FirstOrDefaultAsync(
                        x => x.Id == userId && !x.IsDeleted,
                        cancellationToken);

                if (user == null)
                    return null;

                var roles = await _userManager.GetRolesAsync(user);

                return new UserDetailsDto
                {
                    Id = user.Id,
                    AvatarStoragePath = user.AvatarMediaFile?.StoragePath,

                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,

                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,

                    Roles = roles.ToList(),

                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,

                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get user details. UserId: {UserId}",
                    userId);

                throw;
            }
        }

        // PERSONAL
        public async Task<UserPersonalDetailsDto?> GetUserPersonalDetailsAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.SocialLinks)
                    .FirstOrDefaultAsync(
                        u => u.Id == userId && !u.IsDeleted,
                        cancellationToken);

                if (user == null)
                    return null;

                return new UserPersonalDetailsDto
                {
                    Id = user.Id,
                    DisplayName = user.NormalizedUserName,
                    Address = user.AddressDetail,
                    ProvinceCode = user.ProvinceCode,
                    WardCode = user.WardCode,
                    BirthDate = user.DateOfBirth,
                    Gender = user.Gender,

                    SocialLinks = user.SocialLinks
                        .Select(x => new UserSocialLinkDto
                        {
                            Type = x.Provider,
                            Url = x.Url
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get user personal details. UserId: {UserId}",
                    userId);

                throw;
            }
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

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var createResult = await _userManager.CreateAsync(
                    user,
                    request.Password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        createResult.Errors.Select(x => x.Description));

                    _logger.LogWarning(
                        "Failed to create user account. UserName: {UserName}. Errors: {Errors}",
                        userName,
                        errors);

                    throw new InvalidOperationException(
                        $"Không thể tạo tài khoản: {errors}");
                }

                // Gán Role mặc định User trong transaction hiện tại.
                await AddRoleInternalAsync(
                    user,
                    defaultRole,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "User account created. UserId: {UserId}",
                user.Id);

            // Chỉ ghi ActivityLog sau khi tạo tài khoản thành công.
            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.CreateData,
                    "User",
                    user.Id,
                    $"Created user account: {user.UserName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log user creation activity. UserId: {UserId}",
                    user.Id);
            }

            return user;
        }

        private async Task AddRoleInternalAsync(
    AppUser user,
    string role,
    CancellationToken cancellationToken = default)
        {
            role = role.Trim();

            if (!await _roleManager.RoleExistsAsync(role))
                throw new InvalidOperationException(
                    $"Role '{role}' không tồn tại.");

            if (await _userManager.IsInRoleAsync(user, role))
                return;

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Không thể gán vai trò '{role}': {errors}");
            }
        }

        public async Task UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
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
            if (activeChanged)
            {
                user.PermissionVersion++;
                user.TokenVersion++;
            }
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description));
                _logger.LogError(
            "Failed to update user account. UserId: {UserId}. Errors: {Errors}",
            userId,
            errors);
                throw new InvalidOperationException(
                    $"Không thể cập nhật tài khoản: {errors}");
            }

            // Vô hiệu hóa các cache liên quan khi trạng thái tài khoản đổi.
            

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account updated. UserId: {UserId}",
                userId);
            if (phoneChanged || activeChanged)
            {
                try
                {
                    await _activityLogger.LogAsync(
                        ActivityType.UpdateData,
                        "User",
                        userId,
                        "Updated user account information.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to log user update activity. UserId: {UserId}",
                        userId);
                }
            }
            if (activeChanged)
            {
                try
                {
                    await _securityLogger.LogAsync(
                        request.IsActive
                            ? SecurityActionType.Enable
                            : SecurityActionType.Disable,
                        true,
                        request.IsActive
                            ? "User account was enabled."
                            : "User account was disabled.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to log user account status security event. UserId: {UserId}",
                        userId);
                }
            }
        }

        public async Task<string> UpdateAvatarAsync(string userId, IFormFile file,
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
            catch (Exception ex)
            {
                // Upload mới không được sử dụng nếu cập nhật DB thất bại.
                try
                {
                    await _mediaService.DeleteAsync(newMedia, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to clean up uploaded avatar. UserId: {UserId}", userId);
                }
                _logger.LogError(ex, "Failed to update user avatar. UserId: {UserId}", userId);
                throw;
            }

            // Chỉ xử lý avatar cũ sau khi cập nhật thành công.
            if (oldAvatarId.HasValue && oldAvatarId.Value != newMedia.Id)
            {
                await TryDeleteUnusedMediaAsync(oldAvatarId.Value, cancellationToken);
            }
            try
            {
                await _activityLogger.LogAsync(ActivityType.UpdateData, "User",
                    userId,
                    "Updated user avatar.");
            }
            catch (Exception ex)
            {
                // Không làm thất bại thao tác upload đã hoàn tất chỉ vì lỗi ghi ActivityLog.
                _logger.LogWarning(
                    ex,
                    "Failed to log user avatar update activity. UserId: {UserId}",
                    userId);
            }
            return $"/uploads/{newMedia.StoragePath}";
        }

        private async Task TryDeleteUnusedMediaAsync(long mediaFileId, CancellationToken cancellationToken)
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

            // Xóa bản ghi DB trước.
            try
            {
                _dbContext.MediaFiles.Remove(media);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete unused media record. MediaFileId: {MediaFileId}",
                    mediaFileId);

                throw;
            }

            // Xóa file vật lý sau khi DB đã xóa thành công.
            try
            {
                await _mediaService.DeleteAsync(media, cancellationToken);
                _logger.LogDebug(
            "Successfully deleted unused media file. MediaFileId: {MediaFileId}",
            mediaFileId);
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

        public async Task LockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;

            // Không thực hiện lại nếu tài khoản đã bị khóa.
            if (await _userManager.IsLockedOutAsync(user))
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.Lock);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user lock transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to lock user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account locked successfully. UserId: {UserId}",
                userId);
        }

        public async Task UnlockAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;

            // Không thực hiện nếu tài khoản hiện không bị khóa.
            if (!await _userManager.IsLockedOutAsync(user))
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.UnLock);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user unlock transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to unlock user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account unlocked successfully. UserId: {UserId}",
                userId);
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

            if (user == null || user.IsDeleted)
                return;

            // Không thực hiện lại nếu tài khoản đã bị vô hiệu hóa.
            if (!user.IsActive)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.IsActive = false;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.Disable);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user disable transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to disable user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account disabled successfully. UserId: {UserId}",
                userId);
        }

        public async Task EnableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;

            // Không thực hiện lại nếu tài khoản đã được kích hoạt.
            if (user.IsActive)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.IsActive = true;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.Enable);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user enable transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to enable user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account enabled successfully. UserId: {UserId}",
                userId);
        }

        public async Task<ServiceResult> DeactivateAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return ServiceResult.Fail("Tài khoản không tồn tại.");

            if (!user.IsActive)
                return ServiceResult.Fail("Tài khoản đã bị vô hiệu hóa.");

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.IsActive = false;
                user.IsDeleted = true;
                user.Status = EntityStatus.Deleted;
                user.PermissionVersion++;
                user.TokenVersion++;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Không thể vô hiệu hóa tài khoản: {errors}");
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user deactivation transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to deactivate user account. UserId: {UserId}",
                    userId);

                return ServiceResult.Fail(
                    "Không thể vô hiệu hóa tài khoản. Vui lòng thử lại.");
            }

            await InvalidateUserCache(userId);

            try
            {
                await _securityLogger.LogAsync(
                    SecurityActionType.Disable,
                    true,
                    $"User account deactivated. UserId: {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log user deactivation security event. UserId: {UserId}",
                    userId);
            }

            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    "Deactivated user account.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log user deactivation activity. UserId: {UserId}",
                    userId);
            }

            _logger.LogInformation(
                "User account deactivated successfully. UserId: {UserId}",
                userId);

            return ServiceResult.Success(
                "Đã vô hiệu hóa tài khoản thành công.");
        }

        public async Task<ServiceResult> UpdatePersonalAsync(
    string userId,
    UpdateUserPersonalDto request,
    CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users
                .Include(u => u.SocialLinks)
                .FirstOrDefaultAsync(
                    u => u.Id == userId && !u.IsDeleted,
                    cancellationToken);

            if (user == null)
                return ServiceResult.Fail("Không tìm thấy tài khoản.");

            // Bỏ qua các dòng mạng xã hội chưa nhập URL.
            var socialLinks = (request.SocialLinks ?? new List<UserSocialLinkDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .Select(x => new UserSocialLinkDto
                {
                    Type = x.Type?.Trim(),
                    Url = x.Url?.Trim()
                })
                .ToList();

            var allowedProviders = new HashSet<string>(
                new[]
                {
            "Facebook", "Instagram", "TikTok", "YouTube",
            "Zalo", "LinkedIn", "X", "Website"
                },
                StringComparer.OrdinalIgnoreCase);

            // Kiểm tra nền tảng và URL.
            if (socialLinks.Any(x =>
                string.IsNullOrWhiteSpace(x.Type) ||
                !allowedProviders.Contains(x.Type) ||
                !Uri.TryCreate(x.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps)))
            {
                return ServiceResult.Fail(
                    "Liên kết mạng xã hội không hợp lệ.");
            }

            // Không cho phép trùng nền tảng.
            if (socialLinks
                .GroupBy(x => x.Type!, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1))
            {
                return ServiceResult.Fail(
                    "Không được chọn trùng nền tảng mạng xã hội.");
            }

            try
            {
                user.NormalizedUserName = request.DisplayName?.Trim();
                user.AddressDetail = request.Address?.Trim();
                user.ProvinceCode = request.ProvinceCode?.Trim();
                user.WardCode = request.WardCode?.Trim();
                user.DateOfBirth = CommonHelper.DateTimeToUtc(request.BirthDate);
                user.Gender = request.Gender;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                // Xóa các liên kết không còn được gửi lên.
                var submittedProviders = socialLinks
                    .Select(x => x.Type!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var removedLinks = user.SocialLinks
                    .Where(x => !submittedProviders.Contains(x.Provider))
                    .ToList();

                _dbContext.UserSocialLinks.RemoveRange(removedLinks);

                // Thêm mới hoặc cập nhật liên kết hiện có.
                foreach (var item in socialLinks)
                {
                    var existing = user.SocialLinks.FirstOrDefault(x =>
                        string.Equals(
                            x.Provider,
                            item.Type,
                            StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        user.SocialLinks.Add(new UserSocialLink
                        {
                            UserId = user.Id,
                            Provider = item.Type!,
                            Url = item.Url!
                        });
                    }
                    else
                    {
                        existing.Url = item.Url!;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user personal information. UserId: {UserId}",
                    userId);

                return ServiceResult.Fail(
                    "Cập nhật thông tin cá nhân thất bại.");
            }

            // Ghi nhận hoạt động sau khi cập nhật database thành công.
            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    "Updated user personal information.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log user personal information update. UserId: {UserId}",
                    userId);
            }

            _logger.LogInformation(
                "User personal information updated successfully. UserId: {UserId}",
                userId);

            return ServiceResult.Success(
                "Cập nhật thông tin cá nhân thành công.");
        }

        //SECURITY
        public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                // Reset password.
                // Identity tự cập nhật PasswordHash và SecurityStamp.
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var result = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Đổi mật khẩu lỗi: {errors}");
                }

                // Tăng PermissionVersion và ghi SecurityLog.
                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.ResetPassword);

                // Thu hồi refresh token trên tất cả thiết bị.
                await _refreshTokenService.RevokeAllUserTokensAsync(
                    userId,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback password reset transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to reset user password. UserId: {UserId}",
                    userId);

                throw;
            }

            // Chỉ xóa cache sau khi transaction thành công.
            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User password reset successfully. UserId: {UserId}",
                userId);
        }

        #endregion END-USER



        #region ROLE
        public async Task<List<UserRoleOptionDto>> GetRoleOptionsAsync(
    CancellationToken cancellationToken = default)
        {
            try
            {
                return await _roleManager.Roles
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new UserRoleOptionDto
                    {
                        Value = x.Id,
                        Name = x.Name
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get role options.");
                throw;
            }
        }

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
            await using var transaction =
    await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await _userManager.AddToRoleAsync(user, role);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description));

                    _logger.LogError(
                        "Failed to assign role. UserId: {UserId}, Role: {Role}, Errors: {Errors}",
                        userId,
                        role,
                        errors);

                    throw new InvalidOperationException(errors);
                }

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.RoleAssigned);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback role assignment transaction. UserId: {UserId}, Role: {Role}",
                        userId,
                        role);
                }

                _logger.LogError(
                    ex,
                    "Role assignment transaction failed. UserId: {UserId}, Role: {Role}",
                    userId,
                    role);

                throw;
            }

            // Chỉ xóa cache sau khi transaction thành công.
            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "Role assigned successfully. UserId: {UserId}, Role: {Role}",
                userId,
                role);

            // ActivityLog
            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    $"Assigned role '{role}' to user.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log role assignment activity. UserId: {UserId}, Role: {Role}",
                    userId,
                    role);
            }
        }
        
        public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("User not found.");

            role = role.Trim();

            if (!await _userManager.IsInRoleAsync(user, role))
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(errors);
                }

                await IncreasePermissionVersionInternalAsync(
                    user,
                    SecurityActionType.RoleRemoved);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback role removal transaction. UserId: {UserId}, Role: {Role}",
                        userId,
                        role);
                }

                _logger.LogError(
                    ex,
                    "Failed to remove role. UserId: {UserId}, Role: {Role}",
                    userId,
                    role);

                throw;
            }

            // Chỉ xóa cache sau khi transaction thành công.
            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "Role removed successfully. UserId: {UserId}, Role: {Role}",
                userId,
                role);

            // ActivityLog
            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    $"Removed role '{role}' from user.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log role removal activity. UserId: {UserId}, Role: {Role}",
                    userId,
                    role);
            }
        }

        public async Task ReplaceRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
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
            // Không có thay đổi thì không cần cập nhật version hoặc ghi log.
            var rolesUnchanged =
                currentRoles.Count == targetRoles.Length &&
                currentRoles.All(r =>
                    targetRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

            if (rolesUnchanged)
                return;
            await using var transaction =
      await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Xóa các role hiện tại.
                if (currentRoles.Count > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles);

                    if (!removeResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join(
                                ", ",
                                removeResult.Errors.Select(e => e.Description)));
                    }
                }

                // Gán các role mới.
                if (targetRoles.Length > 0)
                {
                    var addResult = await _userManager.AddToRolesAsync(
                        user,
                        targetRoles);

                    if (!addResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join(
                                ", ",
                                addResult.Errors.Select(e => e.Description)));
                    }
                }

                // Tăng PermissionVersion và ghi SecurityLog.
                await IncreasePermissionVersionInternalAsync(user, SecurityActionType.RoleReplaced);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Failed to replace user roles. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User roles replaced successfully. UserId: {UserId}",
                userId);

            // ActivityLog sau khi transaction thành công.
            try
            {
                await _activityLogger.LogAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    "User roles were replaced.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log user role replacement activity. UserId: {UserId}",
                    userId);
            }
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

        #endregion END-ROLE

        #region PERMISSION

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

            try
            {
                await _securityLogger.LogAsync(
                    action,
                    true,
                    $"Permission version updated for user '{user.Id}'.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log permission version update. UserId: {UserId}",
                    user.Id);
            }
        }
        
        private async Task InvalidateUserCache(string userId)
        {
            await _cache.RemoveAsync(CacheKeys.UserPermission(userId));
            await _cache.RemoveAsync(CacheKeys.UserRoles(userId));
        }
        #endregion Permission

        public async Task<List<UserSessionDto>> GetUserSessionsAsync(string userId, string? currentSessionId,
        CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;

                return await _dbContext.UserSessions
                    .AsNoTracking()
                    .Where(x =>
                        x.UserId == userId &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
                    .OrderByDescending(x => x.LastActivityAt)
                    .Select(x => new UserSessionDto
                    {
                        Id = x.Id.ToString(),
                        DeviceName = x.DeviceName,
                        UserAgent = x.UserAgent,
                        IpAddress = x.IpAddress,
                        CreatedAt = x.CreatedAt,
                        LastActivityAt = x.LastActivityAt,
                        ExpiresAt = x.ExpiresAt,
                        IsCurrent = currentSessionId != null &&
                                    x.Id.ToString() == currentSessionId
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get user sessions. UserId: {UserId}",
                    userId);

                throw;
            }
        }

        public async Task<int> RevokeUserSessionAsync(
    string userId,
    Guid sessionId,
    CancellationToken cancellationToken = default)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(
                    x => x.Id == userId && !x.IsDeleted,
                    cancellationToken);

            if (!userExists)
                throw new KeyNotFoundException("Không tìm thấy người dùng.");

            var now = DateTime.UtcNow;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var session = await _dbContext.UserSessions
                    .FirstOrDefaultAsync(
                        x => x.Id == sessionId && x.UserId == userId,
                        cancellationToken);

                if (session == null)
                    throw new KeyNotFoundException("Không tìm thấy phiên đăng nhập.");

                if (session.RevokedAt != null)
                    throw new InvalidOperationException("Phiên đăng nhập đã được thu hồi.");

                session.RevokedAt = now;
                session.RevokedReason = "RevokedByAdmin";

                var revokedTokens = await _dbContext.RefreshTokens
                    .Where(x => x.SessionId == sessionId && !x.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, "SessionRevokedByAdmin"),
                        cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "User session revoked by admin. UserId: {UserId}, SessionId: {SessionId}, RevokedTokens: {RevokedTokens}",
                    userId,
                    sessionId,
                    revokedTokens);

                return revokedTokens;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Failed to revoke user session. UserId: {UserId}, SessionId: {SessionId}",
                    userId,
                    sessionId);

                throw;
            }
        }
    }
}
