using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    public class WishlistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
