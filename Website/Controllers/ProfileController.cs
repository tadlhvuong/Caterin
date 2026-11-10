using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs;
using Shared.DTOs.Profile;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Requests;
namespace Website.Controllers
{
    [Authorize]
    [Route("profile")]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUser;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IAuthService _authService;

        private readonly ILogSafetyService _logSafetyService;
        private readonly ILogger<ProfileController> _logger;
        public ProfileController(AppDbContext dbContext, UserManager<AppUser> userManager, ICurrentUserService currentUser,
            SignInManager<AppUser> signInManager, IAuthService authService,
IAuthenticationSchemeProvider schemeProvider, ILogSafetyService logSafetyService, ILogger<ProfileController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _currentUser = currentUser;
            _signInManager = signInManager;
            _schemeProvider = schemeProvider;
            _authService = authService;
            _logSafetyService = logSafetyService;
            _logger = logger;
        }
        public async Task<IActionResult> Index(string tab = "personal")
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            ViewBag.Tab = tab;
            ViewBag.DisplayName = user.DisplayName;
            ViewBag.Email = user.Email;
            ViewBag.HasPassword = await _userManager.HasPasswordAsync(user);
            var avatarUrl = user.Avatar;

            if (user.AvatarMediaFileId.HasValue)
            {
                var mediaFile = await _dbContext.MediaFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == user.AvatarMediaFileId.Value);

                if (mediaFile != null)
                {
                    avatarUrl = Url.Content($"~/uploads/{mediaFile.StoragePath}");
                }
            }

            ViewBag.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl)
                ? Url.Content("~/images/Default/default-user.png")
                : avatarUrl;

            return View();
        }
        [HttpGet("personal-info")]
        public async Task<IActionResult> PersonalInfo()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            var model = new UserProfileDto
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender
            };

            if (IsAjaxRequest)
                return PartialView("_PersonalInfo", model);

            return RedirectToAction(nameof(Index), new { tab = "personal" });
        }

        [HttpPost("personal-info")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonalInfo(UserProfileDto model)
        {
            if (!ModelState.IsValid)
                return PartialView("_PersonalInfo", model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            var phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim();

            var phoneChanged = !string.Equals(
                user.PhoneNumber,
                phoneNumber,
                StringComparison.Ordinal);

            user.DisplayName = model.DisplayName.Trim();
            user.PhoneNumber = phoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;

            if (phoneChanged)
                user.PhoneNumberConfirmed = false;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return PartialView("_PersonalInfo", model);
            }

            model.DisplayName = user.DisplayName;
            model.Email = user.Email;
            model.PhoneNumber = user.PhoneNumber;
            model.DateOfBirth = user.DateOfBirth;
            model.Gender = user.Gender;
            model.SavedSuccessfully = true;

            if (IsAjaxRequest)
                return PartialView("_PersonalInfo", model);

            return RedirectToAction(nameof(Index), new { tab = "personal" });
        }

        [HttpGet("addresses")]
        public IActionResult Address()
        {
            if (IsAjaxRequest)
                return PartialView("_Addresses");

            return RedirectToAction(nameof(Index), new { tab = "addresses" });
        }
        [HttpGet("change-password")]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            var model = new ProfilePasswordViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user)
            };

            if (IsAjaxRequest)
                return PartialView("_ChangePassword", model);

            return RedirectToAction(nameof(Index), new { tab = "password" });
        }

        [HttpPost("change-password")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ChangePassword(
    ProfilePasswordViewModel model,
    CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            // Luôn lấy trạng thái mật khẩu từ DB, không tin dữ liệu form.
            var hasPassword = await _userManager.HasPasswordAsync(user);
            model.HasPassword = hasPassword;

            if (hasPassword && string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(
                    nameof(model.CurrentPassword),
                    "Vui lòng nhập mật khẩu hiện tại.");
            }

            if (!ModelState.IsValid)
                return PartialView("_ChangePassword", model);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = model.CurrentPassword ?? string.Empty,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            };

            var result = await _authService.ChangePasswordAsync(
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    var field = error.Field switch
                    {
                        nameof(ChangePasswordRequest.CurrentPassword)
                            => nameof(model.CurrentPassword),

                        nameof(ChangePasswordRequest.NewPassword)
                            => nameof(model.NewPassword),

                        nameof(ChangePasswordRequest.ConfirmPassword)
                            => nameof(model.ConfirmPassword),

                        _ => string.Empty
                    };

                    ModelState.AddModelError(field, error.Message);
                }

                return PartialView("_ChangePassword", model);
            }

            // AuthService đã revoke toàn bộ session và refresh token.
            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ClearProfileAuthCookies();

            TempData["PasswordChanged"] = true;

            var loginUrl = Url.Action(
                "Login",
                "Auth",
                new { area = "Admin" })!;

            // HTMX cần điều hướng toàn trang, không swap trang Login
            // vào bên trong #profile-content.
            if (IsAjaxRequest)
            {
                Response.Headers["HX-Redirect"] = loginUrl;
                return Ok();
            }

            return Redirect(loginUrl);
        }
        [HttpGet("orders")]
        public IActionResult Orders()
        {
            if (IsAjaxRequest)
                return PartialView("_Orders");

            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }
        [HttpGet("order-details")]
        public IActionResult OrderDetails()
        {
            return View();
        }

        [HttpGet("connections")]
        public async Task<IActionResult> Connections()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            var linkedLogins = await _userManager.GetLoginsAsync(user);

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var supportedProviders = new Dictionary<string, (string Name, string Icon)>
            {
                ["Google"] = ("Google", "ti ti-brand-google"),
                ["Facebook"] = ("Facebook", "ti ti-brand-facebook")
            };

            var providers = schemes
                .Where(x => supportedProviders.ContainsKey(x.Name))
                .Select(x =>
                {
                    var info = supportedProviders[x.Name];

                    return new ExternalConnectionDto
                    {
                        Provider = x.Name,
                        DisplayName = info.Name,
                        Icon = info.Icon,
                        IsConnected = linkedLogins.Any(login =>
                            string.Equals(
                                login.LoginProvider,
                                x.Name,
                                StringComparison.OrdinalIgnoreCase))
                    };
                })
                .ToList();

            var model = new ConnectionsViewModel
            {
                Connections = providers
            };

            if (IsAjaxRequest)
                return PartialView("_Connections", model);

            return RedirectToAction(nameof(Index), new { tab = "connections" });
        }
        [HttpPost("connections/{provider}/connect")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-linking")]
        public async Task<IActionResult> ConnectExternalAccount(
    string provider)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            var scheme = await _schemeProvider.GetSchemeAsync(provider);

            if (scheme == null ||
                !new[] { "Google", "Facebook" }.Contains(
                    provider,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Nhà cung cấp không được hỗ trợ.";
                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            var linkedLogins = await _userManager.GetLoginsAsync(user);

            if (linkedLogins.Any(x =>
                string.Equals(x.LoginProvider, provider, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "Tài khoản này đã được kết nối.";
                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            var callbackUrl = Url.Action(
                nameof(ConnectExternalAccountCallback),
                "Profile",
                new { area = "" });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider,
                callbackUrl!,
                user.Id);

            return Challenge(properties, provider);
        }
        [HttpGet("connections/callback")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ConnectExternalAccountCallback(
    string? remoteError = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalAccountLinked,
                    false,
                    $"External account linking failed. Provider callback returned an error. UserId={user.Id}");

                TempData["Error"] = "Không thể kết nối tài khoản. Vui lòng thử lại.";

                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            var loginInfo = await _signInManager.GetExternalLoginInfoAsync(user.Id);

            if (loginInfo == null)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalAccountLinked,
                    false,
                    $"External account linking failed. Invalid external login information. UserId={user.Id}");

                TempData["Error"] = "Phiên kết nối không hợp lệ hoặc đã hết hạn.";

                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            try
            {
                var supportedProviders = new[] { "Google", "Facebook" };

                if (!supportedProviders.Contains(
                    loginInfo.LoginProvider,
                    StringComparer.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Nhà cung cấp không được hỗ trợ.";
                    return RedirectToAction(nameof(Index), new { tab = "connections" });
                }

                // Kiểm tra provider + provider key đã thuộc tài khoản nào chưa.
                var existingUser = await _userManager.FindByLoginAsync(
                    loginInfo.LoginProvider,
                    loginInfo.ProviderKey);

                if (existingUser != null)
                {
                    if (existingUser.Id == user.Id)
                    {
                        TempData["Success"] = "Tài khoản này đã được kết nối.";
                    }
                    else
                    {
                        await _logSafetyService.TryLogSecurityAsync(
                            SecurityActionType.ExternalAccountLinked,
                            false,
                            $"External account linking rejected because provider is linked to another user. Provider={loginInfo.LoginProvider}, UserId={user.Id}");

                        TempData["Error"] =
                            "Tài khoản Google/Facebook này đã được liên kết với một tài khoản Caterin khác.";
                    }

                    return RedirectToAction(nameof(Index), new { tab = "connections" });
                }

                var result = await _userManager.AddLoginAsync(user, loginInfo);

                if (!result.Succeeded)
                {
                    await _logSafetyService.TryLogSecurityAsync(
                        SecurityActionType.ExternalAccountLinked,
                        false,
                        $"External account linking failed. Provider={loginInfo.LoginProvider}, UserId={user.Id}, Errors={string.Join(",", result.Errors.Select(x => x.Code))}");

                    TempData["Error"] = "Không thể liên kết tài khoản. Vui lòng thử lại.";

                    return RedirectToAction(nameof(Index), new { tab = "connections" });
                }

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalAccountLinked,
                    true,
                    $"External account linked successfully. Provider={loginInfo.LoginProvider}, UserId={user.Id}");

                TempData["Success"] = $"Đã kết nối tài khoản {loginInfo.LoginProvider} thành công.";
            }
            catch (Exception ex)
            {
                // Không ghi ProviderKey hoặc token vào log.
                _logger.LogError(
                    ex,
                    "External account linking failed. UserId={UserId}",
                    user.Id);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalAccountLinked,
                    false,
                    $"External account linking encountered an error. Provider={loginInfo.LoginProvider}, UserId={user.Id}");

                TempData["Error"] = "Đã xảy ra lỗi khi kết nối tài khoản.";
            }
            finally
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            }

            return RedirectToAction(nameof(Index), new { tab = "connections" });
        }
        [HttpPost("connections/{provider}/disconnect")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-linking")]
        public async Task<IActionResult> DisconnectExternalAccount(
    string provider)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return HandleUnauthenticatedRequest();
            var supportedProviders = new[] { "Google", "Facebook" };

            if (!supportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Nhà cung cấp không được hỗ trợ.";
                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }
            var linkedLogins = await _userManager.GetLoginsAsync(user);

            var login = linkedLogins.FirstOrDefault(x =>
                string.Equals(
                    x.LoginProvider,
                    provider,
                    StringComparison.OrdinalIgnoreCase));

            if (login == null)
            {
                TempData["Error"] = "Tài khoản này chưa được kết nối.";
                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            // Không cho xóa phương thức đăng nhập cuối cùng nếu user không có mật khẩu.
            if (user.PasswordHash == null && linkedLogins.Count <= 1)
            {
                TempData["Error"] =
                    "Bạn cần có mật khẩu hoặc một phương thức đăng nhập khác trước khi ngắt kết nối tài khoản này.";

                return RedirectToAction(nameof(Index), new { tab = "connections" });
            }

            try
            {
                var result = await _userManager.RemoveLoginAsync(
                    user,
                    login.LoginProvider,
                    login.ProviderKey);

                if (!result.Succeeded)
                {
                    await _logSafetyService.TryLogSecurityAsync(
                        SecurityActionType.ExternalAccountUnlinked,
                        false,
                        $"External account unlinking failed. Provider={login.LoginProvider}, UserId={user.Id}, Errors={string.Join(",", result.Errors.Select(x => x.Code))}");

                    TempData["Error"] = "Không thể ngắt kết nối tài khoản.";
                }
                else
                {
                    await _logSafetyService.TryLogSecurityAsync(
                        SecurityActionType.ExternalAccountUnlinked,
                        true,
                        $"External account unlinked successfully. Provider={login.LoginProvider}, UserId={user.Id}");

                    TempData["Success"] =
                        $"Đã ngắt kết nối tài khoản {login.LoginProvider}.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "External account unlinking failed. UserId={UserId}",
                    user.Id);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalAccountUnlinked,
                    false,
                    $"External account unlinking encountered an error. Provider={login.LoginProvider}, UserId={user.Id}");

                TempData["Error"] = "Đã xảy ra lỗi khi ngắt kết nối tài khoản.";
            }

            return RedirectToAction(nameof(Index), new { tab = "connections" });
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
        private void ClearProfileAuthCookies()
        {
            Response.Cookies.Delete("access_token", new CookieOptions
            {
                Path = "/"
            });

            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                Path = "/"
            });
        }
        private bool IsAjaxRequest =>
    string.Equals(
        Request.Headers["X-Requested-With"],
        "XMLHttpRequest",
        StringComparison.OrdinalIgnoreCase);

        private IActionResult HandleUnauthenticatedRequest()
        {
            if (IsAjaxRequest)
            {
                Response.Headers["X-Auth-Status"] = "Expired";
                return NotFound();
            }

            return Challenge();
        }
    }
}
