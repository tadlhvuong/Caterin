using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Enums;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/role-perms")]
    [PermissionModule("permissions")]
    public class RolePermissionController : Controller
    {
        [HttpGet("role")]
        [PermissionAction(ActionType.View)]
        public IActionResult Role()
        {
            return View();
        }
        [HttpGet("permission")]
        [PermissionAction(ActionType.View)]
        public IActionResult Permission()
        {
            return View();
        }
    }
}
