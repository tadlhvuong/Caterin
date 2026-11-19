using CATERIN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CATERIN.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("home")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }
        [HttpGet("contact")]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet("terms")]
        public IActionResult Terms()
        {
            return View();
        }
        [HttpGet("chinh-sach-bao-mat")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("chinh-sach-giao-hang")]
        public IActionResult ShippingPolicy()
        {
            return View();
        }
        [HttpGet("chinh-sach-doi-tra")]
        public IActionResult ReturnRefundPolicy()
        {
            return View();
        }
        [HttpGet("chinh-sach-thanh-toan")]
        public IActionResult PaymentPolicy()
        {
            return View();
        }
        [HttpGet("faq")]

        public IActionResult FAQ()
        {
            return View();
        }
        [HttpGet("cong-tac")]
        public IActionResult PartnerCenter()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statusCode = 500)
        {
            Response.StatusCode = statusCode;

            return View("~/Views/StatusCode/Index.html", statusCode);
        }
    }
}
