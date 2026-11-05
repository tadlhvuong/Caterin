using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.Constants.Core;
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
using Shared.Services.Email;
using Shared.UserValidation.Interface;
using Shared.UserValidation.Sevices;
using System;
using System.Data;
using static System.Collections.Specialized.BitVector32;

namespace Shared.Services.Authentication
{
    public sealed class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILevelAuthorizationService _levelService;
        private readonly IMediaService _mediaService;
        private readonly IAppCache _cache;
        private readonly IUserEmailService _userEmailService;

        private readonly ILogger<UserService> _logger;
        private readonly ILogSafetyService _logSafetyService;

        public UserService(AppDbContext dbContext, UserManager<AppUser> userManager, IRefreshTokenService refreshTokenService, RoleManager<AppRole> roleManager,
             ICurrentUserService currentUserService, IMediaService mediaService, ILevelAuthorizationService levelService,
             ILogger<UserService> logger, ILogSafetyService logSafetyService,
             IAppCache cache, IUserEmailService userEmailService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
            _currentUserService = currentUserService;
            _levelService = levelService;
            _mediaService = mediaService;
            _userEmailService = userEmailService;
            _logger = logger;
            _logSafetyService = logSafetyService;
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

        public async Task<PagedResult<UserListItemResponse>> GetUsersAsync(UserDataTableRequest request, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Users.AsNoTracking().Where(x => !x.IsDeleted);

            // Tổng số tài khoản chưa bị xóa mềm.
            var totalCount = await query.CountAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;

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
            if (request.IsIdentityLocked.HasValue)
            {
                query = request.IsIdentityLocked.Value
                    ? query.Where(x =>
                        x.LockoutEnabled &&
                        x.LockoutEnd.HasValue &&
                        x.LockoutEnd.Value > now)
                    : query.Where(x =>
                        !x.LockoutEnabled ||
                        !x.LockoutEnd.HasValue ||
                        x.LockoutEnd.Value <= now);
            }

            // Lọc theo vai trò.
            if (!string.IsNullOrWhiteSpace(request.RoleId))
            {
                var roleId = request.RoleId.Trim();

                query = query.Where(x =>
                    x.UserRoles.Any(ur => ur.RoleId == roleId));
            }

            if (request.UserStatus.HasValue)
            {
                query = query.Where(x =>
                    (int)x.Status == request.UserStatus.Value);
            }
            var filteredCount = await query.CountAsync(cancellationToken);

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

                "IsIdentityLocked" => descending
                    ? query.OrderByDescending(x =>
                        x.LockoutEnabled &&
                        x.LockoutEnd.HasValue &&
                        x.LockoutEnd.Value > now)
                    : query.OrderBy(x =>
                        x.LockoutEnabled &&
                        x.LockoutEnd.HasValue &&
                        x.LockoutEnd.Value > now),

                "LastLogin" => descending
                    ? query.OrderByDescending(x => x.LastLogin)
                    : query.OrderBy(x => x.LastLogin),

                _ => descending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)

            };
            // Phân trang an toàn.
            var start = Math.Max(0, request.Start);
            var length = request.Length <= 0
                ? 10
                : Math.Min(request.Length, 100);
            var items = await query
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    User = new UserListItemResponse
                    {
                        Id = x.Id,
                        UserName = x.UserName,
                        Email = x.Email,
                        PhoneNumber = x.PhoneNumber,
                        AvatarUrl = x.AvatarMediaFile != null ? $"/uploads/{x.AvatarMediaFile.StoragePath}" : null,
                        Status = x.Status,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        LastLogin = x.LastLogin
                    },
                    x.LockoutEnabled,
                    x.LockoutEnd
                })
                .ToListAsync(cancellationToken);
            var resultItems = items.Select(x =>
            {
                x.User.IsIdentityLocked =
    x.LockoutEnabled &&
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
                    .ToListAsync(cancellationToken);

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

                await _levelService.EnsureCanManageUserAsync(
                    userId,
                    allowSelf: true,
                    cancellationToken);

                var roles = await _userManager.GetRolesAsync(user);

                return new UserDetailsDto
                {
                    Id = user.Id,
                    AvatarStoragePath = user.AvatarMediaFile?.StoragePath,

                    UserName = user.UserName ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,

                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,

                    Roles = roles.ToList(),

                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,

                    Status = user.Status,
                    IsActive = user.IsActive,
                    IsIdentityLocked = user.LockoutEnabled && user.LockoutEnd.HasValue &&
                        user.LockoutEnd.Value > DateTimeOffset.UtcNow
                };
            }

            catch (UnauthorizedAccessException)
            {
                throw;
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
                await _levelService.EnsureCanManageUserAsync(
    userId,
    allowSelf: true,
    cancellationToken);
                return new UserPersonalDetailsDto
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
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
            catch (UnauthorizedAccessException)
            {
                throw;
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
            if (string.IsNullOrWhiteSpace(request.UserName))
                throw new InvalidOperationException("Tên đăng nhập không được để trống.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email không được để trống.");

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

            try
            {
                await _levelService.EnsureCanManageRoleAsync(defaultRole, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user");

                throw;
            }
            var user = new AppUser
            {
                UserName = userName,
                DisplayName = userName,
                Email = email,
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),

                EmailConfirmed = false,
                IsActive = true,
                IsDeleted = false,
                Status = EntityStatus.Pending,

                PermissionVersion = 1,
                TokenVersion = 1,

                CreatedAt = DateTime.UtcNow,
                CreatedById = _currentUserService.UserId
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var createResult = await _userManager.CreateAsync(user, request.Password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ",
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
            await _logSafetyService.TryLogActivityAsync(
                    ActivityType.CreateData,
                    "User",
                    user.Id,
                    $"Created user account: {user.UserName}");

            return user;
        }

        private async Task AddRoleInternalAsync(
    AppUser user,
    string role,
    CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(role);
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

        public async Task<ServiceResult> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null || user.IsDeleted)
            {
                throw new InvalidOperationException(
                    "Tài khoản không tồn tại.");
            }
            try
            {
                await _levelService.EnsureCanManageUserAsync(userId, allowSelf: false, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            var displayNameChanged = user.DisplayName != request.DisplayName;
            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            var phoneChanged = user.PhoneNumber != phoneNumber;
            var activeChanged = user.IsActive != request.IsActive;

            if (!displayNameChanged && !phoneChanged && !activeChanged)
                return ServiceResult.Success("NoChanges");

            user.DisplayName = request.DisplayName;
            user.PhoneNumber = phoneNumber;
            user.IsActive = request.IsActive;
            if (phoneChanged)
            {
                user.PhoneNumberConfirmed = false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = _currentUserService.UserId;

            if (activeChanged)
            {
                user.PermissionVersion++;
                user.TokenVersion++;
            }
            await using var transaction =  await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {

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

                if (activeChanged && !request.IsActive)
                {
                    await _refreshTokenService.RevokeAllUserTokensAsync(
                        userId, TokenRevocationReasons.UpdatedUser,
                        cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            await InvalidateUserCache(userId);

            _logger.LogInformation(
                "User account updated. UserId: {UserId}",
                userId);
            if (phoneChanged || activeChanged)
            {
                await _logSafetyService.TryLogActivityAsync(
                        ActivityType.UpdateData,
                        "User",
                        userId,
                        "Updated user account information.");
            }
            if (activeChanged)
            {
                await _logSafetyService.TryLogSecurityAsync(
                        request.IsActive ? SecurityActionType.Enable : SecurityActionType.Disable,
                        true,
                        request.IsActive ? "User account was enabled." : "User account was disabled.");
            }
            return ServiceResult.Success("Updated");
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
            try
            {

                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: true,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                try
                {
                    await TryDeleteUnusedMediaAsync(
                        oldAvatarId.Value,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to clean up old avatar. UserId: {UserId}, MediaFileId: {MediaFileId}",
                        userId,
                        oldAvatarId.Value);
                }
            }
            await _logSafetyService.TryLogActivityAsync(ActivityType.UpdateData, "User",
                   userId,
                   "Updated user avatar.");
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

        public async Task UnlockAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            // Không thực hiện nếu không có trạng thái Lockout cần xử lý.
            if (!user.LockoutEnd.HasValue &&
                user.AccessFailedCount == 0)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var lockoutResult =
                    await _userManager.SetLockoutEndDateAsync(user, null);

                if (!lockoutResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", lockoutResult.Errors.Select(x => x.Description)));

                var resetResult =
                    await _userManager.ResetAccessFailedCountAsync(user);

                if (!resetResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", resetResult.Errors.Select(x => x.Description)));

                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;

                await IncreasePermissionVersionInternalAsync(user);
                await transaction.CommitAsync(cancellationToken);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.UnLock,
                    true,
                    $"Permission version updated for user '{user.Id}'.");
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
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.UnlockAccount,
    "User",
    userId,
    "Unlocked user account.");
            _logger.LogInformation(
                "User account unlocked successfully. UserId: {UserId}",
                userId);
        }


        public async Task DisableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                user.TokenVersion++;

                await IncreasePermissionVersionInternalAsync(
                    user);

                await _refreshTokenService.RevokeAllUserTokensAsync(
                    userId, TokenRevocationReasons.AccountDisabled,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.Disable,
        true,
        $"Permission version updated for user '{user.Id}'.");
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
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.DisabledAccount,
    "User",
    userId,
    "Unsuspended user account.");
            _logger.LogInformation(
                "User account disabled successfully. UserId: {UserId}",
                userId);
        }

        public async Task EnableAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                user.TokenVersion++;
                await IncreasePermissionVersionInternalAsync(
                    user);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.Enable,
        true,
        $"Permission version updated for user '{user.Id}'.");
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
            await _logSafetyService.TryLogActivityAsync(
   ActivityType.EnabledAccount,
   "User",
   userId,
   "Unsuspended user account.");
            _logger.LogInformation(
                "User account enabled successfully. UserId: {UserId}",
                userId);
        }
        public async Task SuspendAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            // Không thực hiện lại nếu tài khoản đã bị đình chỉ.
            if (user.Status == EntityStatus.Suspended)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.Status = EntityStatus.Suspended;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;
                user.TokenVersion++;

                await IncreasePermissionVersionInternalAsync(
                    user);

                await _refreshTokenService.RevokeAllUserTokensAsync(
                    userId, TokenRevocationReasons.AccountSuspended,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.Suspend,
        true,
        $"Permission version updated for user '{user.Id}'.");
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user suspend transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to suspend user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);
            await _logSafetyService.TryLogActivityAsync(
   ActivityType.SuspendAccount,
   "User",
   userId,
   "Unsuspended user account.");
            _logger.LogInformation(
                "User account suspended successfully. UserId: {UserId}",
                userId);
        }

        public async Task UnsuspendAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return;
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            // Chỉ gỡ đình chỉ khi tài khoản đang Suspended.
            if (user.Status != EntityStatus.Suspended)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.Status = EntityStatus.Active;
                user.UpdatedById = _currentUserService.UserId;
                user.UpdatedAt = DateTime.UtcNow;
                user.TokenVersion++;

                await IncreasePermissionVersionInternalAsync(
                    user);

                await _refreshTokenService.RevokeAllUserTokensAsync(
                    userId, TokenRevocationReasons.AccountUnSuspended,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.Unsuspend,
        true,
        $"Permission version updated for user '{user.Id}'.");
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user unsuspend transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to unsuspend user account. UserId: {UserId}",
                    userId);

                throw;
            }

            await InvalidateUserCache(userId);
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.UnsuspendAccount,
    "User",
    userId,
    "Unsuspended user account.");
            _logger.LogInformation(
                "User account unsuspended successfully. UserId: {UserId}",
                userId);
        }

        public async Task<ServiceResult> DeactivateAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return ServiceResult.Fail("Tài khoản không tồn tại.");
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            if (user.Status == EntityStatus.Deactivated)
                return ServiceResult.Fail("Tài khoản đã bị vô hiệu hóa.");

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.IsActive = false;
                user.Status = EntityStatus.Deactivated;
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
                await _refreshTokenService.RevokeAllUserTokensAsync(userId,TokenRevocationReasons.AccountDeactivated, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback user deactivation transaction. UserId: {UserId}",
                        userId);
                }

                throw;
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

            await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.Deactivate,
                    true,
                    $"User account deactivated. UserId: {userId}.");

            await _logSafetyService.TryLogActivityAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    "Deactivated user account.");

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
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: true,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                user.DisplayName = request.DisplayName?.Trim();
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
            catch (OperationCanceledException)
            {
                throw;
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
            await _logSafetyService.TryLogActivityAsync(
                    ActivityType.UpdateData,
                    "User",
                    userId,
                    "Updated user personal information.");

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
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                    user);

                // Thu hồi refresh token trên tất cả thiết bị.
                await _refreshTokenService.RevokeAllUserTokensAsync(
                    userId, TokenRevocationReasons.ResetPassword,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.ResetPassword,
        true,
        $"Permission version updated for user '{user.Id}'.");
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

        public async Task<ServiceResult> ResendConfirmEmailForAdminAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InvalidUserId",
                    Message = "User ID is required."
                });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ResendConfirmEmail,
                    false,
                    $"Admin resend confirm email failed. Target user not found. UserId: {userId}");

                return ServiceResult.Fail(new ServiceError
                {
                    Code = "UserNotFound",
                    Message = "User not found."
                });
            }

            if (user.EmailConfirmed)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ResendConfirmEmail,
                    false,
                    $"Admin resend confirm email ignored. Email already confirmed. UserId: {user.Id}");

                return ServiceResult.Fail(new ServiceError
                {
                    Code = "EmailAlreadyConfirmed",
                    Message = "Email has already been confirmed."
                });
            }

            try
            {
                // Kiểm tra quyền quản lý tài khoản theo Level.
                await _levelService.EnsureCanManageUserAsync(
                    user.Id,
                    allowSelf: false,
                    cancellationToken);

                var result = await _userEmailService.SendConfirmEmailAsync(
                    user, cancellationToken);

                if (!result.Succeeded)
                {
                    await _logSafetyService.TryLogSecurityAsync(
                        SecurityActionType.ResendConfirmEmail,
                        false,
                        $"Admin resend confirm email failed. UserId: {user.Id}");

                    return result;
                }

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ResendConfirmEmail,
                    true,
                    $"Admin resend confirm email succeeded. UserId: {user.Id}");

                return ServiceResult.Success(
                    "Confirmation email sent successfully.");
            }
            catch (Exception ex)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ResendConfirmEmail,
                    false,
                    $"Admin resend confirm email exception. UserId: {user.Id}. Error: {ex.Message}");

                throw;
            }
        }
        #endregion END-USER



        #region ROLE
        public async Task<List<UserRoleOptionDto>> GetRoleOptionsAsync(
    CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                if (string.IsNullOrWhiteSpace(currentUserId))
                    throw new UnauthorizedAccessException(
                        "Không xác định được người thao tác.");

                var currentLevel = await _levelService.GetUserLevelAsync(
                    currentUserId,
                    cancellationToken);

                if (!currentLevel.HasValue)
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền thực hiện thao tác này.");

                return await _roleManager.Roles
     .AsNoTracking()
     .Where(x => x.Level > currentLevel.Value)
     .Where(x =>
         !x.IsSystem &&
         x.Name != "Root" &&
         x.Name != "System" &&
         x.Name != "Admin")
     .OrderBy(x => x.Name)
     .Select(x => new UserRoleOptionDto
     {
         Value = x.Id,
         Name = x.Name
     })
     .ToListAsync(cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
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
            if (user == null || user.IsDeleted)
                throw new Exception("Tài khoản không tồn tại.");

            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            role = role.Trim();

            var targetRole = await _roleManager.FindByNameAsync(role);

            if (targetRole == null)
                throw new KeyNotFoundException($"Role '{role}' không tồn tại.");

            if (IsProtectedRole(targetRole))
                throw new UnauthorizedAccessException(
                    "This role cannot be assigned through normal user management.");
            try
            {
                await _levelService.EnsureCanManageUserAsync(
        userId,
        allowSelf: false,
        cancellationToken);

                await _levelService.EnsureCanManageRoleAsync(
                    role,
                    cancellationToken);

            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }

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
                    user);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.RoleAssigned,
        true,
        $"Permission version updated for user '{user.Id}'.");
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
            await _logSafetyService.TryLogActivityAsync(
                   ActivityType.UpdateData,
                   "User",
                   userId,
                   $"Assigned role '{role}' to user.");
        }

        public async Task<ServiceResult> AssignDefaultRoleAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Fail("Tài khoản không hợp lệ.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                return ServiceResult.Fail("Tài khoản không tồn tại.");

            const string defaultRole = "User";

            var role = await _roleManager.FindByNameAsync(defaultRole);

            if (role == null)
                return ServiceResult.Fail(
                    "Vai trò mặc định không tồn tại trong hệ thống.");

            // Tránh gán trùng role.
            if (await _userManager.IsInRoleAsync(user, defaultRole))
                return ServiceResult.Success();

            var result = await _userManager.AddToRoleAsync(user, defaultRole);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => x.Description));

                return ServiceResult.Fail(
                    $"Không thể cấp vai trò mặc định: {errors}");
            }

            await IncreasePermissionVersionAsync(
                user.Id,
                cancellationToken);

            return ServiceResult.Success();
        }

        public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("User not found.");

            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            role = role.Trim();

            var targetRole = await _roleManager.FindByNameAsync(role);

            if (targetRole == null)
                throw new KeyNotFoundException($"Role '{role}' không tồn tại.");

            if (IsProtectedRole(targetRole))
                throw new UnauthorizedAccessException(
                    "This role cannot be removed through normal user management.");
            try
            {
                await _levelService.EnsureCanManageUserAsync(
                userId,
                allowSelf: false,
                cancellationToken);

                await _levelService.EnsureCanManageRoleAsync(
                    role,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
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
                    user);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.RoleRemoved,
        true,
        $"Permission version updated for user '{user.Id}'.");
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
            await _logSafetyService.TryLogActivityAsync(
                   ActivityType.UpdateData,
                   "User",
                   userId,
                   $"Removed role '{role}' from user.");
        }

        public async Task ReplaceRolesAsync(
    string userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            ArgumentNullException.ThrowIfNull(roles);

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            var targetRoles = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Xác định Role cần gỡ và Role cần thêm.
            var rolesToRemove = currentRoles
                .Where(r => !targetRoles.Contains(
                    r,
                    StringComparer.OrdinalIgnoreCase))
                .ToArray();

            var rolesToAdd = targetRoles
                .Where(r => !currentRoles.Contains(
                    r,
                    StringComparer.OrdinalIgnoreCase))
                .ToArray();

            // Không có thay đổi thì không cần kiểm tra quyền,
            // cập nhật PermissionVersion hoặc ghi log.
            if (rolesToRemove.Length == 0 && rolesToAdd.Length == 0)
                return;

            // Kiểm tra quyền quản lý User và Role.
            try
            {
                await _levelService.EnsureCanManageUserAsync(
                    userId,
                    allowSelf: false,
                    cancellationToken);

                // Kiểm tra các Role sẽ bị gỡ.
                foreach (var roleName in rolesToRemove)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);

                    if (role == null)
                        throw new KeyNotFoundException(
                            $"Vai trò '{roleName}' không tồn tại.");

                    if (IsProtectedRole(role))
                        throw new UnauthorizedAccessException(
                            "Protected roles cannot be removed through normal user management.");

                    await _levelService.EnsureCanManageRoleAsync(
                        roleName,
                        cancellationToken);
                }

                // Kiểm tra các Role mới sẽ được gán.
                foreach (var roleName in rolesToAdd)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);

                    if (role == null)
                        throw new KeyNotFoundException(
                            $"Vai trò '{roleName}' không tồn tại.");

                    if (IsProtectedRole(role))
                        throw new UnauthorizedAccessException(
                            "Protected roles cannot be assigned through normal user management.");

                    await _levelService.EnsureCanManageRoleAsync(
                        roleName,
                        cancellationToken);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to validate role replacement permissions. UserId: {UserId}",
                    userId);

                throw;
            }

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Gỡ các Role cũ.
                if (rolesToRemove.Length > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(
                        user,
                        rolesToRemove);

                    if (!removeResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join(
                                ", ",
                                removeResult.Errors.Select(e => e.Description)));
                    }
                }

                // Gán các Role mới.
                if (rolesToAdd.Length > 0)
                {
                    var addResult = await _userManager.AddToRolesAsync(
                        user,
                        rolesToAdd);

                    if (!addResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join(
                                ", ",
                                addResult.Errors.Select(e => e.Description)));
                    }
                }

                // Chỉ tăng PermissionVersion trong transaction.
                await IncreasePermissionVersionInternalAsync(
                    user);

                await transaction.CommitAsync(cancellationToken);
                await _logSafetyService.TryLogSecurityAsync(
        SecurityActionType.RoleReplaced,
        true,
        $"Permission version updated for user '{user.Id}'.");
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
                        "Failed to rollback role replacement transaction. UserId: {UserId}",
                        userId);
                }

                _logger.LogError(
                    ex,
                    "Failed to replace user roles. UserId: {UserId}",
                    userId);

                throw;
            }

            // Chỉ thực hiện sau khi transaction thành công.
            await InvalidateUserCache(userId);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.RoleReplaced,
                true,
                $"Roles replaced for user '{userId}'.");

            await _logSafetyService.TryLogActivityAsync(
                ActivityType.UpdateData,
                "User",
                userId,
                "User roles were replaced.");

            _logger.LogInformation(
                "User roles replaced successfully. UserId: {UserId}",
                userId);
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

            await IncreasePermissionVersionInternalAsync(user);
            await InvalidateUserCache(userId);
        }

        public async Task IncreasePermissionVersionByRoleAsync(string role, CancellationToken cancellationToken = default)
        {
            var roleId = await _dbContext.Roles
    .Where(r => r.Name == role)
    .Select(r => r.Id)
    .FirstOrDefaultAsync(cancellationToken);

            if (roleId == null)
                return;

            var userIds = await _dbContext.UserRoles.Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId).ToListAsync(cancellationToken);

            await _dbContext.Users.Where(u => userIds.Contains(u.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PermissionVersion, u => u.PermissionVersion + 1), cancellationToken);
            await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.RoleBulkUpdated,
                    true,
                    $"PermissionVersion increased for role {role}");
            foreach (var userId in userIds)
            {
                await InvalidateUserCache(userId);
            }
        }

        private async Task IncreasePermissionVersionInternalAsync(AppUser user)
        {
            user.PermissionVersion++;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));

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
                await _levelService.EnsureCanManageUserAsync(
    userId,
    allowSelf: true,
    cancellationToken);
                var now = DateTime.UtcNow;
                Guid? currentId = Guid.TryParse(currentSessionId, out var parsed) ? parsed : null;

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
                        IsCurrent = currentId.HasValue && x.Id == currentId.Value
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
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
            try
            {
                await _levelService.EnsureCanManageUserAsync(
    userId,
    allowSelf: false,
    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }
            var revokedTokens = await _refreshTokenService.RevokeSessionAsync(
                userId,
                sessionId,
                "RevokedByAdmin",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Admin revoked user session. UserId={UserId}, SessionId={SessionId}",
                userId,
                sessionId);
            await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.RevokeSession,
                    true,
                    $"Admin revoked session. UserId: {userId}, SessionId: {sessionId}");
            return revokedTokens;
        }
        public async Task<int> RevokeAllUserSessionsAsync(
    string userId,
    CancellationToken cancellationToken = default)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(
                    x => x.Id == userId && !x.IsDeleted,
                    cancellationToken);

            if (!userExists)
                throw new KeyNotFoundException("Không tìm thấy người dùng.");

            // Kiểm tra cấp bậc người thao tác.
            try
            {
                await _levelService.EnsureCanManageUserAsync(
                userId,
                allowSelf: false,
                cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update user. UserId: {UserId}",
                    userId);

                throw;
            }

            var revokedTokens = await _refreshTokenService.RevokeAllUserTokensAsync(
                userId, TokenRevocationReasons.RevokeAllSession, cancellationToken);

            _logger.LogInformation(
                "Admin revoked all user sessions. UserId={UserId}, RevokedTokens={RevokedTokens}",
                userId,
                revokedTokens);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.RevokeSession,
                true,
                $"Admin revoked all sessions. UserId: {userId}, RevokedTokens: {revokedTokens}");

            return revokedTokens;
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync(
    CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var currentPeriodStart = now.AddDays(-7);
            var previousPeriodStart = now.AddDays(-14);

            var currentUsers = await _dbContext.Users
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedAt >= currentPeriodStart &&
                    x.CreatedAt < now,
                    cancellationToken);

            var previousUsers = await _dbContext.Users
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.CreatedAt >= previousPeriodStart &&
                    x.CreatedAt < currentPeriodStart,
                    cancellationToken);

            var currentSessions = await _dbContext.UserSessions
                .CountAsync(x =>
                    x.CreatedAt >= currentPeriodStart &&
                    x.CreatedAt < now,
                    cancellationToken);

            var previousSessions = await _dbContext.UserSessions
                .CountAsync(x =>
                    x.CreatedAt >= previousPeriodStart &&
                    x.CreatedAt < currentPeriodStart,
                    cancellationToken);

            var currentActiveEvents = await _dbContext.ActivityLogs
                .CountAsync(x =>
                    x.EntityType == "User" &&
                    (x.ActivityType == ActivityType.UnlockAccount ||
                     x.ActivityType == ActivityType.UnsuspendAccount) &&
                    x.CreatedAt >= currentPeriodStart &&
                    x.CreatedAt < now,
                    cancellationToken);

            var previousActiveEvents = await _dbContext.ActivityLogs
                .CountAsync(x =>
                    x.EntityType == "User" &&
                    (x.ActivityType == ActivityType.UnlockAccount ||
                     x.ActivityType == ActivityType.UnsuspendAccount) &&
                    x.CreatedAt >= previousPeriodStart &&
                    x.CreatedAt < currentPeriodStart,
                    cancellationToken);

            var currentPendingEvents = await _dbContext.ActivityLogs
                .CountAsync(x =>
                    x.EntityType == "User" &&
                    x.ActivityType == ActivityType.Register &&
                    x.CreatedAt >= currentPeriodStart &&
                    x.CreatedAt < now,
                    cancellationToken);

            var previousPendingEvents = await _dbContext.ActivityLogs
                .CountAsync(x =>
                    x.EntityType == "User" &&
                    x.ActivityType == ActivityType.Register &&
                    x.CreatedAt >= previousPeriodStart &&
                    x.CreatedAt < currentPeriodStart,
                    cancellationToken);

            return new UserStatisticsDto
            {
                TotalUsers = await _dbContext.Users
                    .CountAsync(x => !x.IsDeleted, cancellationToken),

                TotalUsersChangePercent = CalculateChangePercent(
                    currentUsers, previousUsers),

                ActiveUsers = await _dbContext.Users
                    .CountAsync(x =>
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.Status == EntityStatus.Active &&
                        (!x.LockoutEnabled ||
                         !x.LockoutEnd.HasValue ||
                         x.LockoutEnd.Value <= now),
                        cancellationToken),

                ActiveUsersChangePercent = CalculateChangePercent(
                    currentActiveEvents, previousActiveEvents),

                PendingUsers = await _dbContext.Users
                    .CountAsync(x =>
                        !x.IsDeleted &&
                        x.Status == EntityStatus.Pending,
                        cancellationToken),

                PendingUsersChangePercent = CalculateChangePercent(
                    currentPendingEvents, previousPendingEvents),

                SessionUsers = await _dbContext.UserSessions
    .Where(x =>
        x.RevokedAt == null &&
        x.ExpiresAt > now &&
        !x.User.IsDeleted &&
        x.User.IsActive &&
        x.User.Status == EntityStatus.Active &&
        (!x.User.LockoutEnabled ||
         !x.User.LockoutEnd.HasValue ||
         x.User.LockoutEnd.Value <= now))
    .Select(x => x.UserId)
    .Distinct()
    .CountAsync(cancellationToken),

                SessionUsersChangePercent = CalculateChangePercent(
                    currentSessions, previousSessions)
            };
        }

        private static decimal CalculateChangePercent(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(
                ((decimal)(current - previous) / previous) * 100,
                1);
        }
        private static bool IsProtectedRole(AppRole role)
        {
            return role.IsSystem ||
                   string.Equals(
                       role.Name,
                       "Root",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       role.Name,
                       "System",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       role.Name,
                       "Admin",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
