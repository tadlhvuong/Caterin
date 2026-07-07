using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using System.Linq;
using System.Reflection;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin")]
    [PermissionModule("dashboard")]
    public class HomeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly ILogger _logger;

        private readonly string culture;

        public HomeController(
            UserManager<AppUser> userManager,
            AppDbContext dbContext,
            IPermissionService permissionService,
            ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _permissionService = permissionService;
            _logger = logger; 
        }

        // GET: HomeController
        [HttpGet("")]
        [PermissionAction(ActionType.View)]
        public IActionResult Index()
        {
            var isAuth = User.Identity?.IsAuthenticated;
            var name = User.Identity?.Name;
            var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();

            return Ok(new
            {
                isAuth,
                name,
                claims
            });
            //return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet("dashboard")]
        [PermissionAction(ActionType.View)]
        public IActionResult Dashboard()
        {
            return View();
        }

        //public ActionResult Dashboard()
        //{
        //    return View();
        //}

        //private async Task<List<Menu>> GetMenusAsync(string userId)
        //{
        //    var permissions = await _permissionService.GetPermissionsAsync(userId);

        //    var menus = await _dbContext.Menus
        //        .AsNoTracking()
        //        .OrderBy(x => x.SortOrder)
        //        .ToListAsync();

        //    return menus
        //        .Where(m =>
        //            m.Permission == null ||
        //            permissions.Contains(m.Permission))
        //        .ToList();
        //}
    }
}
