using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
