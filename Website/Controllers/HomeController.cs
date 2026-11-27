using CATERIN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Banner;
using System.Diagnostics;
using System.Threading;

namespace CATERIN.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IBannerService _bannerService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IBannerService bannerService, ILogger<HomeController> logger)
        {
            _bannerService = bannerService;
            _logger = logger;
        }
        [HttpGet("home")]
        public IActionResult Home()
        {
            return RedirectPermanent("/trang-chu");
        }
        [HttpGet("")]
        [HttpGet("trang-chu")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var banners = await _bannerService.GetHomeBannersAsync(cancellationToken);
            return View(banners);
        }
        [HttpGet("gioi-thieu")]
        public IActionResult About()
        {
            return View();
        }
        [HttpGet("lien-he")]
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
