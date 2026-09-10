using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Website.Areas.Admin.Models;

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

        private readonly ILogger<UserController> _logger;
        private readonly IActivityLogger _activityLogger;

        private readonly RoleManager<AppRole> _roleManager;
        public UserController(AppDbContext dbContext, IUserService userService, RoleManager<AppRole> roleManager,
            ILogger<UserController> logger, IActivityLogger activityLogger)
        {
            _dbContext = dbContext;
            _userService = userService;
            _roleManager = roleManager;

            _logger = logger;
            _activityLogger = activityLogger;
        }
        [HttpGet("")]
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            var user = _dbContext.Users.ToList();
            return View(user);
        }
        [HttpGet("user-list")]
        [PermissionAction(ActionType.View)]
        public ActionResult UserList()
        {
            return RedirectToActionPermanent(nameof(Index));
        }
        [Authorize]
        [PermissionAction(ActionType.View)]
        [HttpGet("list")]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.GetUsersAsync(request, cancellationToken);
            return Ok(result);
        }
        [HttpGet("user-details/{id?}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Details(string id, string tab="personal")
        {
            var user = await _userService.FindByIdAsync(id);
            if(user == null) return NotFound();

            var vm = new UserDetailsViewModel
            {
                CurrentTab = tab,
                Id = id,
                UserName = user.UserName!,
                Email = user.Email!,
                ConfirmEmail = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber!,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLogin,
            };
            var AllowedTabs = new List<string> { "personal", "security" };
            if (!AllowedTabs.Contains(tab))
            {
                return NotFound();
            }
            return View(vm);
        }
        [HttpGet("details-personal/{id?}")]
        [PermissionAction(ActionType.View)]
        public IActionResult DetailsPersonal(Guid id)
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Personal");

                return RedirectToAction(nameof(Details), new { id= id, tab = "personal" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
        [HttpGet("details-security/{id?}")]
        [PermissionAction(ActionType.View)]
        public IActionResult DetailsSecurity(Guid id)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Security");

            return RedirectToAction(nameof(Index), new { id = id, tab = "security" });
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
            return View();
        }
        [HttpPost("user-create")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Create)]
        public ActionResult Create(IFormCollection collection)
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


        [HttpGet("user-edit/{id?}")]
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id)
        {
            return View();
        }
        [HttpPost("user-edit/{id?}")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id, IFormCollection collection)
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

        [HttpPost("user-assign-role/{id?}/{role?}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            await _userService.AssignRoleAsync(userId, role);

            return RedirectToAction(nameof(Details), new { id = userId });
        }
        [HttpPost("user-reset-password/{userId?}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> ResetPassword(string userId, ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(userId, request.NewPassword);

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        [HttpGet("roles")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                }).OrderBy(x => x.name).ToListAsync();

            return Ok(roles);
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
    }
}
