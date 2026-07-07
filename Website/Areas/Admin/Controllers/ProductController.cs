using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Product;
using Shared.Enums;
using Shared.Services.Log;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/product")]
    [PermissionModule("products")]
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ActivityLogger _activityLogger;

        private readonly AppDbContext _dbContext;
        public ProductController(ILogger<ProductController> logger, ActivityLogger activityLogger, AppDbContext dbContext) 
        {
            _logger = logger;
            _activityLogger = activityLogger;

            _dbContext = dbContext;
        }
        // GET: HomeController1
        [HttpGet]
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            return View();
        }

        // GET: HomeController1/Details/5
        [HttpGet("details/{id}")]
        [PermissionAction(ActionType.View)]

        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: HomeController1/Create
        [HttpGet("create/{id}")]
        [PermissionAction(ActionType.Create)]
        public ActionResult Create()
        {
            return View();
        }

        // POST: HomeController1/Create
        [HttpPost("create/{id}")]
        [PermissionAction(ActionType.Create)]
        [ValidateAntiForgeryToken]
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

        // GET: HomeController1/Edit/5
        [HttpGet("edit/{id}")]
        [PermissionAction(ActionType.Edit)]
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: HomeController1/Edit/5
        [HttpPost("edit/{id}")]
        [PermissionAction(ActionType.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormCollection collection)
        {
            try
            {
                //var product = _dbContext.Products.FirstOrDefault(x => x.Id == id);
                //await _activityLogger.LogAsync(ActivityType.Update, "Product", product.Id.ToString(), $"Updated product {product.Name}");
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: HomeController1/Delete/5
        [HttpGet("delete/{id}")]
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: HomeController1/Delete/5
        [HttpPost("delete/{id}")]
        [PermissionAction(ActionType.Delete)]
        [ValidateAntiForgeryToken]
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
