using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.User;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Requests.User;
using Shared.Responses.Datatables;
using Shared.Services.Authentication;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using Website.Areas.Admin.Models;
using Website.Areas.Admin.Models.User;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/user")]
    [PermissionModule("Users")]
    public class UserController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        private readonly ILogger<UserController> _logger;
        private readonly ILogSafetyService _logSafetyService;

        public UserController(AppDbContext dbContext, IUserService userService,  
            ILogger<UserController> logger, ILogSafetyService logSafetyService, IAuthService authService)
        {
            _dbContext = dbContext;
            _authService = authService;
            _userService = userService;

            _logger = logger;
            _logSafetyService = logSafetyService;
        }
        
        [HttpGet("")]
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet("user-list")]
        [PermissionAction(ActionType.View)]
        public ActionResult UserList()
        {
            return RedirectToActionPermanent(nameof(Index));
        }

        [PermissionAction(ActionType.View)]
        [HttpPost("get-users")]
        public async Task<IActionResult> GetUsers([FromBody] UserDataTableRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null.");
            }
            var result = await _userService.GetUsersAsync(request);
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,   
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        [HttpGet("user-details/{id?}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Details(string id, string tab = "personal", bool edit = false)
        {
            var allowedTabs = new[]
            {
                "personal",
                "security",
                "billing",
                "notification",
                "connection"
            };

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            if (!allowedTabs.Contains(tab, StringComparer.OrdinalIgnoreCase))
                return NotFound();

            var user = await _userService.GetUserDetailsAsync(id);

            if (user == null)
                return NotFound();

            var vm = new UserDetailsViewModel
            {
                CurrentTab = tab.ToLowerInvariant(),
                Id = user.Id,

                AvatarUrl = !string.IsNullOrWhiteSpace(user.AvatarStoragePath)
                    ? $"/uploads/{user.AvatarStoragePath}"
                    : Url.Content("~/images/Default/default-user.png"),

                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                ConfirmEmail = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Status = user.Status,
                Roles = user.Roles,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLogin,
                IsActive = user.IsActive
            };

            var updateUserVm = new UpdateUserViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                IsActive = user.IsActive
            };

            ViewData["UpdateUserModel"] = updateUserVm;
            ViewData["OpenEditModal"] = edit;

            return View(vm);
        }

        [HttpGet("details-personal/{id}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> DetailsPersonalAsync(string id)
        {
            var user = await _userService.GetUserPersonalDetailsAsync(id);

            if (user == null)
                return NotFound();

            var model = new UpdatePersonalViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Address = user.Address,
                ProvinceCode = user.ProvinceCode,
                WardCode = user.WardCode,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                SocialLinks = user.SocialLinks
            };

            if (model.SocialLinks == null || !model.SocialLinks.Any())
            {
                model.SocialLinks = new List<UserSocialLinkDto>
                {
                    new UserSocialLinkDto()
                };
            }

            return PartialView("_Personal", model);
        }

        [HttpPost("user-details/{id}/update-personal")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> UpdatePersonal(
    string id,
    UpdatePersonalViewModel model,
    CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu gửi lên không hợp lệ."
                });
            }

            var request = new UpdateUserPersonalDto
            {
                DisplayName = model.DisplayName,
                Address = model.Address,
                ProvinceCode = model.ProvinceCode,
                WardCode = model.WardCode,
                BirthDate = model.BirthDate,
                Gender = model.Gender,
                SocialLinks = model.SocialLinks
            };

            var result = await _userService.UpdatePersonalAsync(
                id,
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("{id}/avatar")]
        [PermissionAction(ActionType.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(
    string id,
    IFormFile file,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Tài khoản không hợp lệ." });

            if (file == null || file.Length <= 0)
                return BadRequest(new { message = "Vui lòng chọn ảnh đại diện." });

            try
            {
                var avatarUrl = await _userService.UpdateAvatarAsync(
                    id,
                    file,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    avatarUrl,
                    message = "Cập nhật ảnh đại diện thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error uploading avatar for user {UserId}", id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể cập nhật ảnh đại diện lúc này."
                });
            }
        }

        [HttpGet("details-security/{id}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> DetailsSecurity(string id)
        {
            var userExists = await _dbContext.Users.AnyAsync(x => x.Id == id && !x.IsDeleted);

            if (!userExists)
                return NotFound();

            return PartialView("_Security", new UserDetailsViewModel
            {
                Id = id
            });
        }

        [HttpGet("details-billing")]
        [PermissionAction(ActionType.View)]
        public IActionResult DetailsBilling(Guid id)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Billing");

            return RedirectToAction(nameof(Index), new { id = id, tab = "billing" });
        }

        [HttpGet("details-notification")]
        [PermissionAction(ActionType.View)]
        public IActionResult DetailsNotification(Guid id)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Notification");

            return RedirectToAction(nameof(Index), new { id = id, tab = "notification" });
        }
        [HttpGet("details-connection")]
        [PermissionAction(ActionType.View)]
        public IActionResult DetailsConnection(Guid id)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Connection");

            return RedirectToAction(nameof(Index), new { id = id, tab = "connection" });
        }

        [HttpGet("user-create")]
        [PermissionAction(ActionType.Create)]
        public ActionResult Create()
        {
            return View("_CreateUser");
        }

        [HttpPost("user-create")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Create)]
        public async Task<IActionResult> Create([FromForm] CreateUserRequest request,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ.",
                    errors = GetModelErrors()
                });

            try
            {
                var user = await _userService.CreateUserAsync(request, cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tạo tài khoản thành công.",
                    id = user.Id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error resetting password");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể đặt lại mật khẩu lúc này."
                });
            }
        }

        [HttpGet("user-edit/{id?}")]
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost("user-edit/{id}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Update(string id,[FromForm] UpdateUserViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Yêu cầu không hợp lệ."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng kiểm tra lại thông tin.",
                    errors = GetModelErrors()
                });
            }

            try
            {
                var user = await _userService.FindByIdAsync(
                    id, cancellationToken);

                if (user == null || user.IsDeleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tài khoản không tồn tại."
                    });
                }

                var request = new UpdateUserRequest
                {
                    DisplayName = model.DisplayName,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = model.IsActive
                };

                var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                var noChanges = result.Data == "NoChanges";

                await _logSafetyService.TryLogActivityAsync(
                    ActivityType.UpdateData,
                    "User",
                    id,
                    $"Updated user account: {user.UserName}");

                return Ok(new
                {
                    success = true,
                    noChanges,
                    message = noChanges ? "Thông tin không có thay đổi." : "Cập nhật tài khoản thành công.",
                    data = new
                    {
                        displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null
                            : request.DisplayName.Trim(),
                        phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                            ? null : request.PhoneNumber.Trim(),
                        isActive = request.IsActive
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error updating user {UserId}", id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể cập nhật tài khoản."
                });
            }
        }

        [HttpGet("user-delete/{id?}")]
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost("user-delete/{id?}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [HttpPost("{id}/deactivate")]
        [PermissionAction(ActionType.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new
                {
                    success = false,
                    message = "Tài khoản không hợp lệ."
                });

            var result = await _userService.DeactivateAsync(
                id,
                cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("resend-confirm-email/{id}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> ResendConfirmEmail(
     string id,
     CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tài khoản không hợp lệ."
                });
            }

            var user = await _userService.FindByIdAsync(
                id, cancellationToken);

            if (user == null || user.IsDeleted)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Tài khoản không tồn tại."
                });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email của tài khoản đã được xác minh."
                });
            }

            try
            {
                var result = await _userService.ResendConfirmEmailForAdminAsync(id, cancellationToken);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi lại email xác thực."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resend confirmation email for user {UserId}",
                    id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể gửi email xác thực lúc này."
                });
            }
        }

        [HttpPost("user-assign-role/{userId?}/{role?}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> AssignRole(string userId, string role,
    CancellationToken cancellationToken)
        {
            try
            {
                await _userService.AssignRoleAsync(
                    userId,
                    role,
                    cancellationToken);

                return RedirectToAction(nameof(Details), new { id = userId });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

        }


        [HttpPost("user-reset-password/{userId}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.ResetPassword)]
        public async Task<IActionResult> ResetPassword(string userId, [FromForm] UserResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Mật khẩu không hợp lệ.",
                    errors = GetModelErrors()
                });

            try
            {
                await _userService.ResetPasswordAsync(userId, request.NewPassword);

                //await _logSafetyService.TryLogActivityAsync($"Reset password for user: {userId}");

                await _logSafetyService.TryLogActivityAsync(ActivityType.ChangePass, "User",
                userId, $"Reset password for user: {userId}");

                return Ok(new
                {
                    success = true,
                    message = "Đặt lại mật khẩu thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error resetting password for user {UserId}", userId);

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("user-suspend/{id}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Suspend(string id,
    CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.FindByIdAsync(id,cancellationToken);

                if (user == null || user.IsDeleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tài khoản không tồn tại."
                    });
                }

                await _userService.SuspendAsync(id,cancellationToken);

                var updatedUser = await _userService.FindByIdAsync(id, cancellationToken);
                
                await _logSafetyService.TryLogActivityAsync(
                    ActivityType.SuspendAccount,
                    "User",
                    id,
                    $"Suspended user account: {user.UserName}");

                return Ok(new
                {
                    success = true,
                    message = "Đã tạm ngưng tài khoản.",
                    status = (int)updatedUser.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể tạm ngưng tài khoản."
                });
            }
        }

        [HttpPost("user-activate/{id}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Activate(string id,CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userService.FindByIdAsync(id,cancellationToken);

                if (user == null || user.IsDeleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tài khoản không tồn tại."
                    });
                }

                await _userService.UnsuspendAsync(id, cancellationToken);
                var updatedUser = await _userService.FindByIdAsync(id, cancellationToken);
                await _logSafetyService.TryLogActivityAsync(
                    ActivityType.UnsuspendAccount,
                    "User",
                    id,
                    $"Activated user account: {user.UserName}");

                return Ok(new
                {
                    success = true,
                    message = "Đã kích hoạt tài khoản.",
                    status = (int)updatedUser.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error activating user {UserId}", id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể kích hoạt tài khoản."
                });
            }
        }

        [HttpPost("user-unlock/{id}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Unlock(string id)
        {
            try
            {
                var user = await _userService.FindByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tài khoản không tồn tại."
                    });
                }

                await _userService.UnlockAsync(id);

                await _logSafetyService.TryLogActivityAsync(
                    ActivityType.UnlockAccount,
                    "User",
                    id,
                    $"Unlocked user account: {user.UserName}");

                return Ok(new
                {
                    success = true,
                    message = "Đã mở khóa đăng nhập."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error unlocking user {UserId}", id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể mở khóa tài khoản."
                });
            }
        }

        [HttpGet("roles")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var roles = await _userService.GetRoleOptionsAsync(cancellationToken);

            return Ok(roles.Select(x => new
            {
                value = x.Value,
                name = x.Name
            }));
        }

        [HttpGet("statuses")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetStatuses()
        {
            var statuses = Enum.GetValues<EntityStatus>()
                 .Select(x =>
                 {
                     var member = typeof(EntityStatus).GetMember(x.ToString()).First();
                     var display = member.GetCustomAttribute<DisplayAttribute>();

                     return new
                     {
                         value = (int)x,
                         name = display?.GetShortName() ?? x.ToString()
                     };
                 });

            return Ok(statuses);
        }

        [HttpGet("{userId}/sessions")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetSessions(
     string userId,
     CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            var currentSessionId = User.FindFirstValue(ClaimConstants.SessionId);

            var sessions = await _userService.GetUserSessionsAsync(
                userId,
                currentSessionId,
                cancellationToken);

            return Ok(sessions);
        }

        [HttpPost("{userId}/sessions/{sessionId:guid}/revoke")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> RevokeUserSession(string userId, Guid sessionId,
        CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Tài khoản không hợp lệ.");

            try
            {
                var revokedTokens = await _userService.RevokeUserSessionAsync(
                    userId,
                    sessionId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Đã thu hồi phiên đăng nhập.",
                    revokedRefreshTokens = revokedTokens
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{userId}/sessions/revoke-all")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> RevokeAllSessions(
    string userId,
    CancellationToken cancellationToken)
        {
            try
            {
                var revokedCount = await _userService.RevokeAllUserSessionsAsync(
                    userId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Đã thu hồi {revokedCount} phiên đăng nhập."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to revoke all user sessions. UserId: {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể đăng xuất tất cả thiết bị."
                });
            }
        }

        [HttpGet("statistics")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetStatistics(
    CancellationToken cancellationToken)
        {
            var statistics = await _userService.GetUserStatisticsAsync(
                cancellationToken);

            return Json(statistics);
        }

        private Dictionary<string, string[]> GetModelErrors()
        {
            return ModelState.Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .ToArray());
        }
    }
}
