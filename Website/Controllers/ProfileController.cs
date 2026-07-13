using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Profile;
using Shared.Interfaces.IdentityServices;
namespace Website.Controllers
{
    [Route("profile")]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUser;
        public ProfileController(UserManager<AppUser> userManager, ICurrentUserService currentUser)
        {
            _userManager = userManager;
            _currentUser = currentUser;
        }
        public IActionResult Index(string tab = "personal")
        {
            ViewBag.Tab = tab;
            return View();
        }
        [HttpGet("personal-info")]
        public async Task<IActionResult> PersonerInfo()
        {
            try
            {
                var user = await _userManager.FindByIdAsync(_currentUser.UserId!);

                if (user == null)
                    return NotFound();

                var model = new UserProfileDto
                {
                    UserName = user.UserName!,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber,
                    Avatar = user.Avatar
                };


                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_PersonalInfo", model);

                return RedirectToAction(nameof(Index), new { tab = "personal" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }

        [HttpPost("personal-info")]
        public async Task<IActionResult> PersonalInfo(UserProfileDto model)
        {
            if (!ModelState.IsValid)
                return PartialView("_PersonalInfo", model);

            var user = await _userManager.FindByIdAsync(_currentUser.UserId!);

            user.UserName = model.UserName;
            user.PhoneNumber = model.PhoneNumber;

            await _userManager.UpdateAsync(user);

            var dto = new UserProfileDto
            {
                UserName = user.UserName!,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Avatar = user.Avatar,
                SavedSuccessfully = true
            };
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_PersonalInfo", model);

            return RedirectToAction(nameof(Index), new { tab = "personal" });
            //return PartialView("_PersonalInfo", dto);
        }

        [HttpGet("addresses")]
        public IActionResult Address()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Addresses");

            return RedirectToAction(nameof(Index), new { tab = "address" });
        }
        [HttpGet("change-password")]
        public IActionResult ChangePassword()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_ChangePassword");

            return RedirectToAction(nameof(Index), new { tab = "password" });
        }
        [HttpGet("orders")]
        public IActionResult Orders()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("_Orders");

            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }
        [HttpGet("order-details")]
        public IActionResult OrderDetails()
        {
            return View();
        }

        [HttpGet("dashboard")]
        public IActionResult PartnerDashboard()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Dashboard.cshtml");
        }

        [HttpGet("commission")]
        public IActionResult Commission()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Commission.cshtml");
        }
        [HttpGet("partner")]
        public IActionResult Partner()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Member.cshtml");
        }
        [HttpGet("payment")]
        public IActionResult PaymentPartner()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/PaymentPartner.cshtml");
        }

        [HttpGet("marketing")]
        public IActionResult Marketing()
        {
            // Chỉ định rõ đường dẫn từ thư mục gốc của Views
            return View("~/Views/Profile/Partner/Marketing.cshtml");
        }

    }
}
