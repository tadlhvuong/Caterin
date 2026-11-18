using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    [AllowAnonymous]
    [Route("blog")]
    public class BlogController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("blog-details")]
        public IActionResult Details(string? blogCode)
        {
            //ViewBag.blogCode = blogCode;

            return View();
        }
    }
}
