using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Requests;
using Shared.Services.Log;

namespace Website.Areas.Admin.Controllers
{
    public class MemberController : Controller
    {
        private readonly ILogger<MemberController> _logger;
        private readonly ActivityLogger _activityLogger;

        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        public MemberController(ILogger<MemberController> logger, ActivityLogger activityLogger, 
            AppDbContext dbContext, IAuthService authService, IUserService userService)
        {
            _logger = logger;
            _activityLogger = activityLogger;

            _dbContext = dbContext;
            _authService = authService;
            _userService = userService;
        }
        [HttpPost]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            //await _authService.AssignRoleAsync(userId, role
            await _userService.AssignRoleAsync(userId, role);

            return RedirectToAction(nameof(Details), new { id = userId });
        }
        [HttpPost]
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
        [HttpPost]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> ResetPassword(string userId, ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(userId, request.NewPassword);

            return RedirectToAction(nameof(Details), new { id = userId });
        }
        // GET: MemberController
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            return View();
        }

        // GET: MemberController/Details/5
        [PermissionAction(ActionType.View)]
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: MemberController/Create
        [PermissionAction(ActionType.Create)]
        public ActionResult Create()
        {
            return View();
        }

        // POST: MemberController/Create
        [HttpPost]
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
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: MemberController/Edit/5
        [HttpPost]
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
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MemberController/Delete/5
        [HttpPost]
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
    }
}
