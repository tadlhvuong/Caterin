using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Address()
        {
            return View();
        }
        public IActionResult ChangePassword()
        {
            return View();
        }
        public IActionResult Orders()
        {
            return View();
        }
        public IActionResult OrderDetails()
        {
            return View();
        }

        public IActionResult PartnerDashboard()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Dashboard.cshtml");
        }

        public IActionResult Commission()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Commission.cshtml");
        }
        public IActionResult Partner()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Member.cshtml");
        }
        public IActionResult PaymentPartner()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/PaymentPartner.cshtml");
        }

        public IActionResult Marketing()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Marketing.cshtml");
        }

    }
}
