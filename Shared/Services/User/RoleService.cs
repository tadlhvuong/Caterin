using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.RolePermisison;
using Shared.Enums;
using Shared.Interfaces.Log;
using Shared.Interfaces.User;
using Shared.Responses;
using Shared.UserValidation.Interface;
using Shared.UserValidation.Sevices;
using System.Security;


namespace Shared.Services.User
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _dbContext;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ILevelAuthorizationService _levelService;


        private readonly ILogSafetyService _logSafetyService;
        private readonly ILogger<RoleService> _logger;
        public RoleService(AppDbContext dbContext, RoleManager<AppRole> roleManager,
            ILevelAuthorizationService levelService,
    ILogSafetyService logSafetyService,
    ILogger<RoleService> logger)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _levelService = levelService;

            _logSafetyService = logSafetyService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RoleCardDto>> GetRoleCardsAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _dbContext.Roles
                .AsNoTracking()
                .Select(role => new RoleCardDto
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Description = role.Description,
                    Level = role.Level,
                    IsSystem = role.IsSystem,

                    MemberCount = _dbContext.UserRoles.Count(userRole => userRole.RoleId == role.Id &&
                        _dbContext.Users.Any(user => user.Id == userRole.UserId && !user.IsDeleted)),

                    PermissionCount = _dbContext.RolePermissions
                        .Count(rolePermission => rolePermission.RoleId == role.Id)
                })
                .OrderBy(role => role.Level)
                .ThenBy(role => role.Name)
                .ToListAsync(cancellationToken);

            return roles;
        }

        public async Task<AppRole?> GetByIdAsync(
    string id,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (role == null)
                return null;

            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            return role;
        }

        public async Task<ServiceResult> CreateAsync(CreateRoleDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult.Fail(
                    new ServiceError
                    {
                        Field = nameof(dto.Name),
                        Message = "Role name is required."
                    });
            }

            var roleName = dto.Name.Trim();

            // Không cho tạo role dành riêng của hệ thống.
            if (string.Equals(roleName, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(
                    new ServiceError
                    {
                        Field = nameof(dto.Name),
                        Message = "This role name is reserved."
                    });
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return ServiceResult.Fail(
                    new ServiceError
                    {
                        Field = nameof(dto.Name),
                        Message = "A role with this name already exists."
                    });
            }

            var now = DateTime.UtcNow;
            var normalizedAdminName = _roleManager.NormalizeKey("Admin");

            var adminLevel = await _dbContext.Roles
                .AsNoTracking()
                .Where(x => x.NormalizedName == normalizedAdminName)
                .Select(x => (int?)x.Level)
                .SingleOrDefaultAsync(cancellationToken);

            if (!adminLevel.HasValue)
            {
                return ServiceResult.Fail(
                    new ServiceError
                    {
                        Message = "Admin role was not found."
                    });
            }

            var maxLevel = await _dbContext.Roles
                .AsNoTracking()
                .Select(x => (int?)x.Level)
                .MaxAsync(cancellationToken);

            var newLevel = Math.Max((maxLevel ?? adminLevel.Value) + 1, adminLevel.Value + 1);
            try
            {
                await _levelService.EnsureCanCreateRoleAsync(newLevel, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.RoleDenied,
    false,
    $"Role creation denied. Requested role: {roleName}, requested level: {newLevel}.");

                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to modify this role or assign the specified level."
                });
            }

            var role = new AppRole
            {
                Name = roleName,
                NormalizedName = _roleManager.NormalizeKey(roleName),
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),
                Level = newLevel,
                IsSystem = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                return ServiceResult.Fail(
                    result.Errors.Select(error => new ServiceError
                    {
                        Field = nameof(dto.Name),
                        Message = error.Description
                    }));
            }
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.CreateData,
    "Role",
    role.Id,
    $"Created role '{role.Name}' with level {role.Level}.");

            return ServiceResult.SuccessMessage("Role created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(UpdateRoleDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Id) ||
                string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InvalidRoleData",
                    Field = nameof(dto.Name),
                    Message = "Role ID and name are required."
                });
            }

            var role = await _roleManager.FindByIdAsync(dto.Id);

            if (role == null)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }

            // Bảo vệ các role hệ thống.
            if (role.IsSystem ||
                string.Equals(role.Name, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "SystemRoleProtected",
                    Message = "System roles cannot be modified."
                });
            }

            // Không cho phép chỉnh sửa role Admin.
            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "ProtectedRole",
                    Message = "The Admin role cannot be modified."
                });
            }

            try
            {
                // Kiểm tra quyền quản lý Role hiện tại.
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);

                // Kiểm tra Level mới không được cao hơn hoặc bằng người thao tác.
                await _levelService.EnsureCanManageRoleLevelAsync(
                    dto.Level,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.RoleDenied,
    false,
    $"Role update denied. RoleId: {role.Id}, role: {role.Name}.");

                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to modify this role or assign the specified level."
                });
            }

            var roleName = dto.Name.Trim();

            var existingRole = await _roleManager.FindByNameAsync(roleName);

            if (existingRole != null && existingRole.Id != role.Id)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "DuplicateRoleName",
                    Field = nameof(dto.Name),
                    Message = "A role with this name already exists."
                });
            }
            var oldName = role.Name;
            var oldLevel = role.Level;
            role.Name = roleName;
            role.Level = dto.Level;
            role.Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();
            role.UpdatedAt = DateTime.UtcNow;

            var identityResult = await _roleManager.UpdateAsync(role);

            if (!identityResult.Succeeded)
            {
                return ServiceResult.Fail(
                    identityResult.Errors.Select(error => new ServiceError
                    {
                        Code = error.Code,
                        Field = nameof(dto.Name),
                        Message = error.Description
                    }));
            }
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.UpdateData,
    "Role",
    role.Id,
    $"Updated role '{oldName}' to '{role.Name}'. Level: {oldLevel} -> {role.Level}.");

            return ServiceResult.SuccessMessage("Role updated successfully.");
        }

        public async Task<ServiceResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InvalidRoleId",
                    Field = nameof(id),
                    Message = "Role ID is required."
                });
            }

            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (role == null)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }
            if (role.IsSystem ||
                string.Equals(role.Name, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "System", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "ProtectedRole",
                    Message = "This role cannot be deleted."
                });
            }

            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.RoleDenied,
    false,
    $"Role deletion denied. RoleId: {role.Id}, role: {role.Name}.");
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to delete this role."
                });
            }

            var hasMembers = await _dbContext.UserRoles
                .AnyAsync(x => x.RoleId == id, cancellationToken);

            if (hasMembers)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "RoleHasMembers",
                    Message = "The role still has members. Remove them before deleting the role."
                });
            }

            var rolePermissions = await _dbContext.RolePermissions
                .Where(x => x.RoleId == id)
                .ToListAsync(cancellationToken);

            _dbContext.RolePermissions.RemoveRange(rolePermissions);
            _dbContext.Roles.Remove(role);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.DeleteAccount,
    "Role",
    role.Id,
    $"Deleted role '{role.Name}'.");
           
            return ServiceResult.SuccessMessage("Role deleted successfully.");
        }

        public async Task<ServiceResult<object>> GetPermissionOptionsAsync(
    string roleId,
    CancellationToken cancellationToken = default)
        {
            //const string systemAdminAccessCode = "system.admin.access";

            if (string.IsNullOrWhiteSpace(roleId))
            {
                return ServiceResult<object>.Fail(new ServiceError
                {
                    Code = "InvalidRoleId",
                    Field = nameof(roleId),
                    Message = "Role ID is required."
                });
            }

            var role = await _dbContext.Roles
    .AsNoTracking()
    .Where(x => x.Id == roleId)
    .Select(x => new
    {
        x.Id,
        x.Name
    })
    .FirstOrDefaultAsync(cancellationToken);

            if (role == null)
            {
                return ServiceResult<object>.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }

            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<object>.Fail(new ServiceError
                {
                    Code = "Forbidden",
                    Message = "You do not have permission to view permissions of this role."
                });
            }

            var modules = await _dbContext.CMSModules
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Code,
                    x.Icon
                })
                .ToListAsync(cancellationToken);

            var actions = Enum.GetValues<ActionType>()
                .Select(x => new
                {
                    Value = (int)x,
                    Name = x.ToString()
                })
                .ToList();

            var moduleIds = modules.Select(x => x.Id).ToList();
            var actionNames = actions.Select(x => x.Name).ToList();

            var specialPermission = await _dbContext.Permissions
                .AsNoTracking()
                .Where(x =>
                    x.Code == PermissionCodes.AdminAccess &&
                    moduleIds.Contains(x.ModuleId))
                .Select(x => new
                {
                    moduleId = x.ModuleId,
                    code = x.Code,
                    action = x.Action,
                    name = x.Name
                })
                .FirstOrDefaultAsync(cancellationToken);

            var assigned = await _dbContext.RolePermissions
                .AsNoTracking()
                .Where(x =>
                    x.RoleId == roleId &&
                    moduleIds.Contains(x.Permission.ModuleId) &&
                    (
                        actionNames.Contains(x.Permission.Action) ||
                        x.Permission.Code == PermissionCodes.AdminAccess
                    ))
                .Select(x => new
                {
                    moduleId = x.Permission.ModuleId,
                    action = x.Permission.Action,
                    code = x.Permission.Code
                })
                .ToListAsync(cancellationToken);

            return ServiceResult<object>.Success(new
            {
                modules,
                actions,
                specialPermission,
                assigned
            });
        }

        public async Task<ServiceResult<int>> UpdatePermissionsAsync(
    UpdateRolePermissionsDto dto,
    string? assignedById,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleId))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InvalidRoleId",
                    Field = nameof(dto.RoleId),
                    Message = "Role ID is required."
                });
            }

            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == dto.RoleId,
                    cancellationToken);

            if (role == null)
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }
            // Bảo vệ các Role hệ thống.
            if (role.IsSystem ||
                string.Equals(role.Name, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "SystemRoleProtected",
                    Message = "System roles cannot have their permissions modified."
                });
            }

            // Không cho phép thay đổi quyền của Admin.
            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "ProtectedRole",
                    Message = "The Admin role permissions cannot be modified."
                });
            }
            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.PermissionDenied,
                    false,
                    $"Permission update denied. RoleId: {role.Id}, role: {role.Name}.");

                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to modify permissions of this role."
                });
            }

            // Chuẩn hóa lựa chọn và phân biệt action Access đặc biệt.
            var selected = new HashSet<(int ModuleId, string Action)>();

            foreach (var item in dto.Selections ?? [])
            {
                if (item.ModuleId <= 0 ||
                    string.IsNullOrWhiteSpace(item.Action))
                {
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "InvalidPermissionSelection",
                        Message = "Invalid module or action."
                    });
                }

                var actionName = item.Action.Trim();

                if (string.Equals(
                    actionName,
                    PermisisonActions.AdminAccess,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selected.Add((item.ModuleId, PermisisonActions.AdminAccess));
                    continue;
                }

                if (!Enum.TryParse<ActionType>(
                        actionName,
                        true,
                        out var action) ||
                    !Enum.IsDefined(action))
                {
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "InvalidPermissionSelection",
                        Message = "Invalid module or action."
                    });
                }

                selected.Add((item.ModuleId, action.ToString()));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var modules = await _dbContext.CMSModules.Where(x => x.IsActive).ToListAsync(cancellationToken);

            var activeModuleIds = modules.Select(x => x.Id).ToHashSet();

            if (selected.Any(x => !activeModuleIds.Contains(x.ModuleId)))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InvalidModule",
                    Message = "One or more modules are inactive or invalid."
                });
            }

            var permissions = await _dbContext.Permissions
                 .Where(x => activeModuleIds.Contains(x.ModuleId))
                .ToListAsync(cancellationToken);

            var rootPermission = await _dbContext.Permissions.AsNoTracking().Where(x => x.Code == PermissionCodes.SystemRoot)
                .Select(x => new
                {
                    x.Id,
                    x.ModuleId,
                    x.Action
                }).SingleOrDefaultAsync(cancellationToken);

            if (rootPermission != null && selected.Any(x => x.ModuleId == rootPermission.ModuleId &&
                    string.Equals(x.Action, rootPermission.Action, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "RootPermissionProtected",
                    Message = "Root permission cannot be assigned through normal permission management."
                });
            }

            var adminAccessPermission = permissions.FirstOrDefault(x =>
                string.Equals(
                    x.Code,
                    PermissionCodes.AdminAccess,
                    StringComparison.OrdinalIgnoreCase));

            var selectedAccess = selected
                .Where(x => string.Equals(
                    x.Action,
                    PermisisonActions.AdminAccess,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (selectedAccess.Count > 0)
            {
                if (adminAccessPermission == null ||
                    !adminAccessPermission.IsActive)
                {
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "AdminAccessNotConfigured",
                        Message = "Permission system.admin.access is not configured or inactive."
                    });
                }

                if (selectedAccess.Any(x =>
                    x.ModuleId != adminAccessPermission.ModuleId))
                {
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "InvalidAdminAccessModule",
                        Message = "Invalid system admin access module."
                    });
                }
            }

            var actionNames = Enum.GetNames<ActionType>();

            // Tạo hoặc kích hoạt permission cho các action thông thường.
            foreach (var item in selected.Where(x =>
                !string.Equals(
                    x.Action,
                    PermisisonActions.AdminAccess,
                    StringComparison.OrdinalIgnoreCase)))
            {
                var permission = permissions.FirstOrDefault(x =>
                    x.ModuleId == item.ModuleId &&
                    string.Equals(
                        x.Action,
                        item.Action,
                        StringComparison.OrdinalIgnoreCase));

                if (permission == null)
                {
                    var module = modules.First(x => x.Id == item.ModuleId);

                    var moduleCode = string.IsNullOrWhiteSpace(module.Code)
                        ? $"module-{module.Id}"
                        : module.Code.Trim();

                    permission = new Permission
                    {
                        ModuleId = module.Id,
                        Code = $"{moduleCode.ToLowerInvariant()}.{item.Action.ToLowerInvariant()}",
                        Action = item.Action,
                        Name = $"{module.Name} {item.Action}",
                        Description = $"{item.Action} permission for {module.Name}",
                        IsActive = true,
                        IsSystem = module.IsSystem
                    };

                    _dbContext.Permissions.Add(permission);
                    permissions.Add(permission);
                }
                else
                {
                    permission.IsActive = true;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Chỉ thay các quyền mà giao diện này quản lý.
            var existingLinks = await _dbContext.RolePermissions
                .Include(x => x.Permission)
                .Where(x =>
                    x.RoleId == dto.RoleId &&
                    activeModuleIds.Contains(x.Permission.ModuleId))
                .ToListAsync(cancellationToken);

            var linksToReplace = existingLinks
                .Where(x =>
                    (rootPermission == null ||
                     x.PermissionId != rootPermission.Id) &&
                    (
                        actionNames.Any(name =>
                            string.Equals(
                                name,
                                x.Permission.Action,
                                StringComparison.OrdinalIgnoreCase)) ||
                        string.Equals(
                            x.Permission.Code,
                            PermissionCodes.AdminAccess,
                            StringComparison.OrdinalIgnoreCase)
                    ))
                .ToList();

            _dbContext.RolePermissions.RemoveRange(linksToReplace);

            var assignedAt = DateTime.UtcNow;

            foreach (var item in selected)
            {
                Permission? permission;

                if (string.Equals(
                    item.Action,
                    PermisisonActions.AdminAccess,
                    StringComparison.OrdinalIgnoreCase))
                {
                    permission = adminAccessPermission;
                }
                else
                {
                    permission = permissions.FirstOrDefault(x =>
                        x.ModuleId == item.ModuleId &&
                        string.Equals(
                            x.Action,
                            item.Action,
                            StringComparison.OrdinalIgnoreCase));
                }

                if (permission == null)
                {
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "PermissionNotFound",
                        Message = $"Permission not found: {item.Action}."
                    });
                }

                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = permission.Id,
                    AssignedById = assignedById,
                    AssignedAt = assignedAt
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Làm mới version quyền của tất cả thành viên thuộc role này.
            await _dbContext.Users
                .Where(x => x.UserRoles.Any(ur => ur.RoleId == dto.RoleId))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.PermissionVersion,
                        x => x.PermissionVersion + 1),
                    cancellationToken);
            var permissionCount = await _dbContext.RolePermissions
              .CountAsync(
                  x => x.RoleId == dto.RoleId,
                  cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.PermissionChanged,
    true,
    $"Permissions updated for role '{role.Name}' (RoleId: {role.Id}).");
          
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.UpdateData,
    "Role",
    role.Id,
    $"Updated permissions for role '{role.Name}'. Permission count: {permissionCount}.");
            return ServiceResult<int>.Success(
                permissionCount,
                "Permissions updated successfully.");
        }

        public async Task<ServiceResult<RoleMembersDto>> GetRoleMembersAsync(
    string id,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ServiceResult<RoleMembersDto>.Fail(new ServiceError
                {
                    Code = "InvalidRoleId",
                    Field = nameof(id),
                    Message = "Role ID is required."
                });
            }

            var role = await _dbContext.Roles
    .AsNoTracking()
    .Where(x => x.Id == id)
    .Select(x => new { x.Id, x.Name })
    .FirstOrDefaultAsync(cancellationToken);

            if (role == null)
            {
                return ServiceResult<RoleMembersDto>.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }

            try
            {
                await _levelService.EnsureCanManageRoleAsync(role.Name!, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<RoleMembersDto>.Fail(new ServiceError
                {
                    Code = "Forbidden",
                    Message = "You do not have permission to view members of this role."
                });
            }

            var members = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(x => x.RoleId == id && !x.User.IsDeleted)
                .OrderBy(x => x.User.UserName)
                .Select(x => new RoleMemberDto
                {
                    Id = x.User.Id,
                    UserName = x.User.UserName,
                    Email = x.User.Email,
                    Avatar = x.User.Avatar,
                    IsActive = x.User.IsActive,
                    IsLocked = x.User.IsLocked,
                    LastLogin = x.User.LastLogin
                })
                .ToListAsync(cancellationToken);

            var data = new RoleMembersDto
            {
                Total = members.Count,
                Members = members
            };

            return ServiceResult<RoleMembersDto>.Success(data);
        }

        public async Task<ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>> GetAvailableRoleMembersAsync(
    string id,
    string? keyword,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>.Fail(
                    new ServiceError
                    {
                        Code = "InvalidRoleId",
                        Field = nameof(id),
                        Message = "Role ID is required."
                    });
            }

            var role = await _dbContext.Roles
    .AsNoTracking()
    .Where(x => x.Id == id)
    .Select(x => new { x.Id, x.Name })
    .FirstOrDefaultAsync(cancellationToken);

            if (role == null)
            {
                return ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>.Fail(
                    new ServiceError
                    {
                        Code = "RoleNotFound",
                        Message = "Role was not found."
                    });
            }

            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>.Fail(
                    new ServiceError
                    {
                        Code = "Forbidden",
                        Message = "You do not have permission to view available members for this role."
                    });
            }

            var query = _dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .Where(u => !_dbContext.UserRoles.Any(ur =>
                    ur.RoleId == id && ur.UserId == u.Id));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();

                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(term)) ||
                    (u.Email != null && u.Email.Contains(term)));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .Select(u => new AvailableRoleMemberDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Avatar = u.Avatar,
                    IsActive = u.IsActive,
                    IsLocked = u.IsLocked
                })
                .Take(50)
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>
                .Success(users);
        }

        public async Task<ServiceResult<int>> AssignMembersAsync(
    AssignRoleMembersDto dto,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleId))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InvalidRoleId",
                    Field = nameof(dto.RoleId),
                    Message = "Role is required."
                });
            }

            var userIds = (dto.UserIds ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "NoUsersSelected",
                    Field = nameof(dto.UserIds),
                    Message = "Select at least one account."
                });
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == dto.RoleId, cancellationToken);
            if (role == null)
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role was not found."
                });
            }
            // Bảo vệ các Role hệ thống.
            if (role.IsSystem == null ||
                string.Equals(role.Name, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "SystemRoleProtected",
                    Message = "Members cannot be assigned to a system role."
                });
            }

            // Không cho phép thay đổi thành viên của Admin.
            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "ProtectedRole",
                    Message = "Members cannot be assigned to the Admin role."
                });
            }
            try
            {
                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.RoleDenied,
    false,
    $"Role member assignment denied. RoleId: {role.Id}, role: {role.Name}.");
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to assign members to this role."
                });
            }

            if (role.IsSystem)
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "SystemRoleProtected",
                    Message = "Members cannot be assigned to a system role."
                });
            }
            var validUserIds = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            if (validUserIds.Count != userIds.Count)
            {
                return ServiceResult<int>.Fail(new ServiceError
                {
                    Code = "InvalidUsers",
                    Field = nameof(dto.UserIds),
                    Message = "Some accounts do not exist or have been deleted."
                });
            }
            foreach (var userId in validUserIds)
            {
                try
                {
                    await _levelService.EnsureCanManageUserAsync(
                        userId,
                        allowSelf: false,
                        cancellationToken);
                }
                catch (UnauthorizedAccessException)
                {
                    await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.RoleDenied,
            false,
            $"Role member assignment denied. RoleId: {role.Id}, UserId: {userId}.");
                    return ServiceResult<int>.Fail(new ServiceError
                    {
                        Code = "InsufficientRoleLevel",
                        Message = "You do not have permission to assign this role to one or more selected users."
                    });
                }
            }
           
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingUserIds = await _dbContext.UserRoles
                .Where(x => x.RoleId == dto.RoleId &&
                            validUserIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);

            var existingSet = existingUserIds.ToHashSet();

            var users = await _dbContext.Users
                .Where(u => validUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var assignedCount = 0;

            foreach (var user in users)
            {
                if (existingSet.Contains(user.Id))
                    continue;

                _dbContext.UserRoles.Add(new AppUserRole
                {
                    UserId = user.Id,
                    RoleId = dto.RoleId
                });

                user.PermissionVersion++;
                assignedCount++;
            }
            if (assignedCount == 0)
            {
                return ServiceResult<int>.Success(
                    0,
                    "All selected accounts are already assigned to this role.");
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            await _logSafetyService.TryLogSecurityAsync(
    SecurityActionType.RoleAssigned,
    true,
    $"Assigned {assignedCount} member(s) to role '{role.Name}' (RoleId: {role.Id}).");
            await _logSafetyService.TryLogActivityAsync(
    ActivityType.UpdateData,
    "Role",
    role.Id,
    $"Assigned {assignedCount} member(s) to role '{role.Name}'.");
            return ServiceResult<int>.Success(
                assignedCount,
                "Accounts assigned successfully.");
        }

        public async Task<ServiceResult> RemoveMemberAsync(
    string roleId,
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role is required."
                });
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "UserNotFound",
                    Message = "User is required."
                });
            }

            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

            if (role == null)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "RoleNotFound",
                    Message = "Role not found."
                });
            }
            // Bảo vệ các Role hệ thống.
            if (role.IsSystem ||
                string.Equals(role.Name, "Root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role.Name, "System", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "SystemRoleProtected",
                    Message = "Members cannot be removed from a system role."
                });
            }

            // Không cho phép thay đổi thành viên của Admin.
            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "ProtectedRole",
                    Message = "Members cannot be removed from the Admin role."
                });
            }
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId && !x.IsDeleted,
                    cancellationToken);

            if (user == null)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "UserNotFound",
                    Message = "User not found."
                });
            }

            var userRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(
                    x => x.RoleId == roleId && x.UserId == userId,
                    cancellationToken);

            if (userRole == null)
            {
                return ServiceResult.Fail(new ServiceError
                {
                    Code = "UserRoleNotFound",
                    Message = "User is not a member of this role."
                });
            }

            // Không được quản lý role ngang cấp hoặc cao hơn.
            try
            {
                await _levelService.EnsureCanManageUserAsync(
                    userId,
                    allowSelf: false,
                    cancellationToken);

                await _levelService.EnsureCanManageRoleAsync(
                    role.Name!,
                    cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                await _logSafetyService.TryLogSecurityAsync(SecurityActionType.RoleDenied,
                    false,
                    $"Role member removal denied. RoleId: {role.Id}, UserId: {userId}.");

                return ServiceResult.Fail(new ServiceError
                {
                    Code = "InsufficientRoleLevel",
                    Message = "You do not have permission to remove this user from the role."
                });
            }

            // Không cho gỡ tài khoản Root khỏi role System.
            if (role.Name == "System")
            {
                var rootPermissionId = await _dbContext.Permissions
                    .Where(p => p.Code == PermissionCodes.SystemRoot)
                    .Select(p => (int?)p.Id)
                    .SingleOrDefaultAsync(cancellationToken);

                if (rootPermissionId.HasValue)
                {
                    var isRoot = await (
                        from ur in _dbContext.UserRoles.AsNoTracking()
                        join rp in _dbContext.RolePermissions.AsNoTracking()
                            on ur.RoleId equals rp.RoleId
                        where ur.UserId == userId
                              && rp.PermissionId == rootPermissionId.Value
                        select ur.UserId
                    ).AnyAsync(cancellationToken);

                    if (isRoot)
                    {
                        return ServiceResult.Fail(new ServiceError
                        {
                            Code = "CannotRemoveRootFromSystem",
                            Message = "Cannot remove the Root account from the System role."
                        });
                    }
                }
            }

            _dbContext.UserRoles.Remove(userRole);

            // Đánh dấu quyền của người dùng đã thay đổi.
            user.PermissionVersion++;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.RoleRemoved, true,
                $"Removed user '{userId}' from role '{role.Name}' (RoleId: {role.Id}).");

            await _logSafetyService.TryLogActivityAsync(
                ActivityType.UpdateData,
                "UserRole",
                userId,
                $"Removed user '{userId}' from role '{role.Name}'.");
            return ServiceResult.SuccessMessage(
                "Xóa thành viên thành công.");
        }
    }
}
