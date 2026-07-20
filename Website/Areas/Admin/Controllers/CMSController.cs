using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        //// GET: CMSController/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: CMSController/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: CMSController/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: CMSController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: CMSController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: CMSController/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: CMSController/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
