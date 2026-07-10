using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Payment()
        {
            return View();
        }
        public IActionResult Success()
        {
            return View();
        }
    }
}
