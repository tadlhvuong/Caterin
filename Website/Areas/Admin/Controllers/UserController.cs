using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.DTOs.Profile;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Services.Log;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/user")]
    [PermissionModule("Users")]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly IActivityLogger _activityLogger;

        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        public UserController(ILogger<UserController> logger, IActivityLogger activityLogger, 
            AppDbContext dbContext, IAuthService authService, IUserService userService)
        {
            _logger = logger;
            _activityLogger = activityLogger;

            _dbContext = dbContext;
            _authService = authService;
            _userService = userService;
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
        // GET: MemberController/Details/5
        [HttpGet("user-details/{id?}")]
        [PermissionAction(ActionType.View)]
        public ActionResult Details(Guid id, string tab="personal")
        {
            var vm = new UserDetailsViewModel
            {
                Id = id,
                CurrentTab = tab
            };
            var AllowedTabs = new List<string> { "personal", "security" };
            if (!AllowedTabs.Contains(tab))
            {
                return NotFound();
            }
            return View(vm);
            //return View(modelId);
        }
        //[HttpGet("details-content/{id?}")]
        //[PermissionAction(ActionType.View)]
        //public IActionResult DetailsContent(Guid id, string tab)
        //{
        //    return tab switch
        //    {
        //        "security" => PartialView("_Security"),
        //        "billing" => PartialView("_Billing"),
        //        "notification" => PartialView("_Notification"),
        //        "connection" => PartialView("_Connection"),
        //        _ => PartialView("_Personal")
        //    };
        //}
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
        // GET: MemberController/Create
        [HttpGet("user-create")]
        [PermissionAction(ActionType.Create)]
        public ActionResult Create()
        {
            return View();
        }

        // POST: MemberController/Create
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

        // GET: MemberController/Edit/5
        [HttpGet("user-edit/{id?}")]
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: MemberController/Edit/5
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

        // GET: MemberController/Delete/5
        [HttpGet("user-delete/{id?}")]
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MemberController/Delete/5
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
            //await _authService.AssignRoleAsync(userId, role
            await _userService.AssignRoleAsync(userId, role);

            return RedirectToAction(nameof(Details), new { id = userId });
        }
        [HttpPost("user-change-password")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var result = await _authService.ChangePasswordAsync(request);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(request);
            }

            return RedirectToAction(nameof(Details));

        }
        [HttpPost("user-reset-password/{userId?}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> ResetPassword(string userId, ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(userId, request.NewPassword);

            return RedirectToAction(nameof(Details), new { id = userId });
        }

    }
}
