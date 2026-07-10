using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
