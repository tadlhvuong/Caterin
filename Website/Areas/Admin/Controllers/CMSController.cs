using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/CMS")]
    [PermissionModule("Menu")]
    public class CMSController : Controller
    {
        private readonly AppDbContext _dbContent;

        public CMSController(AppDbContext dbContext) {
            _dbContent = dbContext;
        }
        // GET: CMSController
        [HttpGet]
        [PermissionAction(ActionType.View)]
        public ActionResult Menu()
        {
            var menu = _dbContent.Menus.FirstOrDefault();
            return View();
        }
    }
}
