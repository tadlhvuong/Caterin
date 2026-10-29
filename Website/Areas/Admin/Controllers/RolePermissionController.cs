using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.RolePermisison;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.User;
using Shared.Services.Authentication;
using System;
using System.Data;
using System.Security.Claims;
using System.Threading;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/role-perms")]
    [PermissionModule("roles")]
    public class RolePermissionController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IRoleService _roleService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;

        public RolePermissionController(AppDbContext dbContext, IRoleService roleService,
            ICurrentUserService currentUserService, IUserService userService, IPermissionService permissionService)
        {
            _dbContext = dbContext;
            _roleService = roleService;
            _currentUserService = currentUserService;
            _userService = userService;
            _permissionService = permissionService;
        }

        // =========================
        // Views
        // =========================

        [HttpGet("role")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Role(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            ViewBag.CurrentUserIsRoot = !string.IsNullOrWhiteSpace(userId)
                && await _permissionService.IsRootAsync(userId, _currentUserService.PermissionVersion, cancellationToken);


            return View();
        }

        [HttpGet("permission")]
        [PermissionAction(ActionType.View)]
        public IActionResult Permission()
        {
            return View();
        }

        // =========================
        // Role API
        // =========================

        // GET: /admin/role-perms/roles
        [HttpGet("roles-list")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetRoles(
            CancellationToken cancellationToken)
        {
            var roles = await _roleService.GetRoleCardsAsync(cancellationToken);

            return Ok(roles);
        }

        // GET: /admin/role-perms/roles/{id}
        [HttpGet("roles/{id}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetRole(
            string id,
            CancellationToken cancellationToken)
        {
            var role = await _roleService.GetByIdAsync(id, cancellationToken);

            if (role == null)
            {
                return NotFound(new
                {
                    message = "Role not found."
                });
            }

            return Ok(new
            {
                role.Id,
                role.Name,
                role.Description,
                role.Level,
                role.IsSystem
            });
        }

        // POST: /admin/role-perms/create
        [HttpPost("create")]
        [PermissionAction(ActionType.Create)]
        public async Task<IActionResult> CreateRole(
    [FromBody] CreateRoleDto dto,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _roleService.CreateAsync(
                dto, cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Unable to create role.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = result.Message ?? "Role created successfully."
            });
        }

        // PUT: /admin/role-perms/api/roles/{id}
        [HttpPut("update/{id}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> UpdateRole(
    string id,
    [FromBody] UpdateRoleDto dto,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!string.Equals(id, dto.Id, StringComparison.Ordinal))
            {
                return BadRequest(new
                {
                    message = "Vai trò không hợp lệ"
                });
            }

            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var currentUserLevel = await _userService.GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            var result = await _roleService.UpdateAsync(
                dto,
                currentUserLevel,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không có vai trò hợp lệ"
                    });
                }

                return BadRequest(new
                {
                    message = "Cập nhật vai trò thất bại",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = result.Message ?? "Sửa vai trò thành công."
            });
        }

        [HttpDelete("delete/{id}")]
        [PermissionAction(ActionType.Delete)]
        public async Task<IActionResult> DeleteRole(
    string id,
    CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var currentUserLevel = await _userService.GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            var result = await _roleService.DeleteAsync(
                id,
                currentUserLevel,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                if (result.Errors.Any(x =>
                    x.Code == "UserRoleNotFound" ||
                    x.Code == "InsufficientRoleLevel"))
                {
                    return Forbid();
                }

                return BadRequest(new
                {
                    message = "Xóa vai trò thất bại.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = result.Message ?? "Xóa vai trò thành công."
            });
        }

        // GET: /admin/role-perms/permission-options
        [HttpGet("permission-options")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetPermissionOptions(
    [FromQuery] string roleId,
    CancellationToken cancellationToken)
        {
            var result = await _roleService.GetPermissionOptionsAsync(
                roleId,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                return BadRequest(new
                {
                    message = "Không thể tải danh sách quyền.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(result.Data);
        }

        [HttpPut("permissions/assign")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> UpdateRolePermissions(
    [FromBody] UpdateRolePermissionsDto dto,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var currentUserLevel = await _userService.GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            var assignedById =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            var result = await _roleService.UpdatePermissionsAsync(
                dto,
                currentUserLevel,
                assignedById,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                if (result.Errors.Any(x =>
                    x.Code == "InsufficientRoleLevel" ||
                    x.Code == "UserRoleNotFound"))
                {
                    return Forbid();
                }

                return BadRequest(new
                {
                    message = "Lưu quyền hạn thất bại.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = "Lưu quyền hạn thành công.",
                permissionCount = result.Data
            });
        }

        [HttpGet("roles/{id}/members")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetRoleMembers(
    string id,
    CancellationToken cancellationToken)
        {
            var result = await _roleService.GetRoleMembersAsync(
                id,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                return BadRequest(new
                {
                    message = "Không thể tải danh sách thành viên.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                total = result.Data!.Total,
                members = result.Data.Members.Select(x => new
                {
                    id = x.Id,
                    userName = x.UserName,
                    email = x.Email,
                    avatar = x.Avatar,
                    isActive = x.IsActive,
                    isLocked = x.IsLocked,
                    lastLogin = x.LastLogin
                })
            });
        }

        [HttpGet("roles/{id}/available-members")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetAvailableRoleMembers(
     string id,
     [FromQuery] string? keyword,
     CancellationToken cancellationToken)
        {
            var result = await _roleService.GetAvailableRoleMembersAsync(
                id,
                keyword,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                return BadRequest(new
                {
                    message = "Không thể tải danh sách thành viên khả dụng.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(result.Data!.Select(x => new
            {
                id = x.Id,
                userName = x.UserName,
                email = x.Email,
                avatar = x.Avatar,
                isActive = x.IsActive,
                isLocked = x.IsLocked
            }));
        }

        [HttpPost("roles/assign-members")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> AssignRoleMembers(
    [FromBody] AssignRoleMembersDto dto,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var currentUserLevel = await _userService.GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            var result = await _roleService.AssignMembersAsync(
                dto,
                currentUserLevel,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "RoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy vai trò."
                    });
                }

                if (result.Errors.Any(x =>
                    x.Code == "InsufficientRoleLevel" ||
                    x.Code == "UserRoleNotFound"))
                {
                    return Forbid();
                }

                return BadRequest(new
                {
                    message = "Gán thành viên vào vai trò thất bại.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = "Gán tài khoản thành công.",
                assignedCount = result.Data
            });
        }

        [HttpDelete("roles/{roleId}/members/{userId}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> RemoveRoleMember(
    string roleId,
    string userId,
    CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var currentUserLevel = await _userService.GetUserLevelAsync(
                currentUserId,
                cancellationToken);

            var result = await _roleService.RemoveMemberAsync(
                roleId,
                userId,
                currentUserLevel,
                cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x =>
                    x.Code == "RoleNotFound" ||
                    x.Code == "UserNotFound" ||
                    x.Code == "UserRoleNotFound"))
                {
                    return NotFound(new
                    {
                        message = result.Errors.First().Message
                    });
                }

                if (result.Errors.Any(x => x.Code == "InsufficientRoleLevel"))
                    return Forbid();

                if (result.Errors.Any(x => x.Code == "CannotRemoveRootFromSystem"))
                {
                    return BadRequest(new
                    {
                        message = "Không thể gỡ tài khoản Root khỏi role System."
                    });
                }

                return BadRequest(new
                {
                    message = "Gỡ thành viên khỏi vai trò thất bại.",
                    errors = result.Errors.Select(error => new
                    {
                        error.Code,
                        error.Field,
                        error.Message
                    })
                });
            }

            return Ok(new
            {
                message = result.Message ?? "Gỡ thành viên thành công."
            });
        }

    }
}
