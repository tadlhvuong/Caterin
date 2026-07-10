using System.Diagnostics;
using CATERIN.Models;
using Microsoft.AspNetCore.Mvc;

namespace CATERIN.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ShippingPolicy()
        {
            return View();
        }
        public IActionResult ReturnRefundPolicy()
        {
            return View();
        }
        public IActionResult PaymentPolicy()
        {
            return View();
        }
        public IActionResult FAQ()
        {
            return View();
        }
        public IActionResult PartnerCenter()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
