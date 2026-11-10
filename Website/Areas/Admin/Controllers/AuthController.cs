using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Requests.Auth;
using Shared.Responses;
using Shared.Services.Email;
using System.Security.Claims;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("[area]/auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _dbContext;
        private readonly JwtSetting _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly IEmailActionService _emailActionService;

        private readonly ILogger<AuthController> _logger;
        private readonly ILogSafetyService _logSafetyService;

        public AuthController(IAuthService authService, SignInManager<AppUser> signInManager,
            AppDbContext dbContext, IOptions<JwtSetting> jwtSettings, 
            ILogger<AuthController> logger, ILogSafetyService logSafetyService, IEmailActionService emailActionService)
        {
            _authService = authService;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _emailActionService = emailActionService;

            _logger = logger;
            _logSafetyService = logSafetyService;
        }
        [AllowAnonymous]
        public ActionResult Index()
        {
            return RedirectToAction("login", "auth");
        }

        #region login
        [AllowAnonymous]
        [HttpGet("login")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("Page: Login");
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { Area = "Admin" });
            }
            return View(
            new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);
            var result = await _authService.LoginAsync(
                    new LoginRequest
                    {
                        Email = model.Email,
                        Password = model.Password,
                        IsRemember = model.RememberMe
                    }, cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SetAuthCookiesAsync(result);

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl!);
            }

            _logger.LogInformation(
                "User {UserName} logged in successfully.",
                result.UserName);

            return RedirectToAction(
                "Index",
                "Home",
                new { area = "Admin" });
        }

        #endregion login

        #region regsiter

        [AllowAnonymous]
        [HttpGet("register")]
        public IActionResult Register()
        {
            _logger.LogInformation("Page: Register Admin");
            return View();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("Register Admin");
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _authService.RegisterAsync(
                new RegisterRequest
                {
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    UserName = model.UserName,
                    AcceptTerms = model.AcceptTerms
                });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(ConfirmEmail));
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public ActionResult ConfirmEmail()
        {
            _logger.LogInformation("Page: Verify Register Admin");
            var email = TempData["Email"] as string;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResendConfirmEmailViewModel
            {
                Email = email!,
                Message = TempData["Resent"] as string
            });
        }

        [AllowAnonymous]
        [HttpPost("resend-confirm-email")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendConfirmEmail(ResendConfirmEmailViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(nameof(ConfirmEmail), model);

            var result = await _authService.ResendConfirmEmailAsync(model.Email, cancellationToken);

            if (!result.Succeeded)
            {
                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Gửi email xác thực",
                    Message = "Không thể gửi email xác thực. Vui lòng thử lại sau."
                });
            }
            TempData["Email"] = model.Email;
            TempData["Resent"] = "Đã gửi lại email xác thực. Vui lòng kiểm tra hộp thư.";
            return RedirectToAction(nameof(ConfirmEmail));
        }
        #endregion register

        #region forgot password
        [AllowAnonymous]
        [HttpGet("forgot-password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ForgotPassword()
        {
            _logger.LogInformation("Page: Forgot password");
            return View();
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ForgotPassword(
    ForgotPasswordViewModel model,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _authService.ForgotPasswordAsync(
                    new ForgotPasswordRequest
                    {
                        Email = model.Email
                    },
                    cancellationToken);

                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }

                TempData["Feedback"] = true;

                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password request failed.");

                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Đã xảy ra lỗi",
                    Message = "Không thể xử lý yêu cầu của bạn. Vui lòng thử lại sau."
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("confirm-forgot-password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ForgotPasswordConfirmation()
        {
            if (TempData["Feedback"] is not true)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }
            _logger.LogInformation("Page: Forgotpassword confirmation");
            return View("Feedback", new FeedbackViewModel
            {
                Type = FeedbackType.Success,
                Title = "Quên mật khẩu",
                Message = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu."
            });
        }

        #endregion forgot password

        #region reset password
        [AllowAnonymous]
        [HttpGet("reset-password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ResetPassword(
    [FromQuery] string? key,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                return RedirectToAction(nameof(Login));

            var actionResult = await _emailActionService.GetValidAsync(
                key,
                EmailActionType.ResetPassword,
                cancellationToken);

            if (!actionResult.Succeeded || actionResult.Data == null)
            {
                TempData["Error"] =
                    actionResult.Errors.FirstOrDefault()?.Message
                    ?? "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";

                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                Key = key
            });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ResetPassword(
    ResetPasswordViewModel model,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrWhiteSpace(model.Key))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Liên kết đặt lại mật khẩu không hợp lệ.");

                return View(model);
            }

            var request = new ResetPasswordRequest
            {
                NewPassword = model.Password
            };

            var result = await _authService.ResetPasswordByTokenAsync(
                model.Key,
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.SignOutAsync();
            ClearAuthCookies();

            TempData["ResetPasswordSuccess"] = true;

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet("reset-password-confirm")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ResetPasswordConfirmation()
        {
            if (TempData["ResetPasswordSuccess"] is not true)
                return RedirectToAction(nameof(Login));

            return View("Feedback", new FeedbackViewModel
            {
                Type = FeedbackType.Success,
                Title = "Đặt lại mật khẩu",
                Message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập bằng mật khẩu mới."
            });
        }

        #endregion reset password

        #region Change password

        [Authorize]
        [HttpGet("change-password")]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [Authorize]
        [HttpPost("change-password")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("password")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ChangePassword(
    ChangePasswordViewModel model,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.Password,
                ConfirmPassword = model.ConfirmPassword
            };

            var result = await _authService.ChangePasswordAsync(
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            ClearAuthCookies();

            return RedirectToAction(nameof(ChangePasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet("change-password-confirm")]
        public IActionResult ChangePasswordConfirmation()
        {
            return View("Feedback", new FeedbackViewModel
            {
                Title = "Đổi mật khẩu",
                Message = "Mật khẩu đã được thay đổi thành công. Vì lý do bảo mật, bạn đã được đăng xuất. Vui lòng đăng nhập lại."
            });
        }

        #endregion Change password

        #region Logout

        [Authorize]
        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _authService.LogoutAsync(
                    refreshToken,
                    cancellationToken);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ClearAuthCookies();

            return RedirectToAction("Login");
        }

        [HttpPost("logout-all")]
        [Authorize]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> LogoutAll(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _authService.LogoutAllAsync(
                    userId,
                    cancellationToken);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ApplicationScheme);

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ClearAuthCookies();

            return RedirectToAction("Login");
        }

        #endregion Logout

        [AllowAnonymous]
        [HttpGet("e/{key}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> EmailAction(
    string key,
    CancellationToken cancellationToken)
        {
            var actionResult = await _emailActionService.GetValidAsync(
                key,
                cancellationToken);

            if (!actionResult.Succeeded || actionResult.Data == null)
            {
                TempData["Error"] =
                    "Liên kết không hợp lệ hoặc đã hết hạn.";

                return RedirectToAction(nameof(Login));
            }

            var emailAction = actionResult.Data;

            switch (emailAction.Type)
            {
                case EmailActionType.ConfirmEmail:
                    return View("ConfirmEmailAction", new ConfirmEmailActionViewModel
                    {
                        Key = key
                    });

                case EmailActionType.ResetPassword:
                    return RedirectToAction(
                        nameof(ResetPassword),
                        new { key });

                default:
                    return RedirectToAction(nameof(Login));
            }
        }
        [AllowAnonymous]
        [HttpPost("confirm-email-action")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ConfirmEmailAction(
    ConfirmEmailActionViewModel model,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(model.Key))
            {
                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Xác thực email thất bại",
                    Message = "Liên kết không hợp lệ."
                });
            }

            var actionResult = await _emailActionService.GetValidAsync(
                model.Key,
                EmailActionType.ConfirmEmail,
                cancellationToken);

            if (!actionResult.Succeeded || actionResult.Data == null)
            {
                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Xác thực email thất bại",
                    Message = "Liên kết không hợp lệ hoặc đã hết hạn."
                });
            }

            var result = await _authService.ConfirmEmailAsync(
                actionResult.Data,
                cancellationToken);

            if (!result.Succeeded)
            {
                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Xác thực email thất bại",
                    Message = "Không thể xác thực email. Vui lòng thử lại."
                });
            }

            return View("Feedback", new FeedbackViewModel
            {
                Type = FeedbackType.Success,
                Title = "Xác thực email",
                Message = "Bạn đã xác thực email thành công."
            });
        }
        [AllowAnonymous]
        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View("Feedback", new FeedbackViewModel
            {
                Type = FeedbackType.Danger,
                Title = "Không có quyền truy cập",
                Message = "Bạn không có quyền truy cập chức năng này.",
                ButtonUrl = "/",
                ButtonText = "Đăng nhập"
            });
        }

        //[HttpPost("save-permission")]
        //public async Task<IActionResult> SavePermission(string roleId, List<int> permissionIds)
        //{
        //    var old = _dbContext.RolePermissions.Where(x => x.RoleId == roleId);

        //    _dbContext.RolePermissions.RemoveRange(old);

        //    _dbContext.RolePermissions.AddRange(permissionIds.Select(id => new RolePermission
        //        {
        //            RoleId = roleId,
        //            PermissionId = id
        //        }));

        //    await _dbContext.SaveChangesAsync();

        //    _cache.Remove($"perm:role:{roleId}");

        //    return View();
        //}

        #region LoginEx
        [AllowAnonymous]
        [HttpPost("external-login")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public IActionResult ExternalLogin(LoginExViewModel model)
        {
            var provider = model.Provider.Trim();

            provider = provider.Equals(
                "google",
                StringComparison.OrdinalIgnoreCase)
                    ? "Google"
                    : provider.Equals(
                        "facebook",
                        StringComparison.OrdinalIgnoreCase)
                            ? "Facebook"
                            : string.Empty;
            if (string.IsNullOrWhiteSpace(provider))
            {
                TempData["Error"] = "Phương thức đăng nhập không hợp lệ.";
                return RedirectToAction(nameof(Login));
            }

            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Auth",
                new { area = "Admin", returnUrl = model.ReturnUrl });

            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            properties.IsPersistent = false;

            properties.Items["RememberMe"] = model.RememberMe.ToString();

            return Challenge(properties, provider);
        }
        [AllowAnonymous]
        [HttpGet("external-login-callback")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ExternalLoginCallback(
    string? returnUrl,
    string? remoteError = null,
    CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                _logger.LogWarning("External login failed: {Error}", remoteError);

                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] =
                    "Đăng nhập bằng Google/Facebook thất bại. Vui lòng thử lại.";

                return RedirectToAction(nameof(Login));
            }

            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();

            if (loginInfo == null)
            {
                _logger.LogWarning("ExternalLoginInfo not found.");

                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] =
                    "Không lấy được thông tin đăng nhập hoặc phiên đăng nhập đã hết hạn.";

                return RedirectToAction(nameof(Login));
            }

            var rememberMe = false;

            if (loginInfo.AuthenticationProperties?.Items.TryGetValue(
                    "RememberMe",
                    out var rememberMeValue) == true)
            {
                bool.TryParse(rememberMeValue, out rememberMe);
            }
            var avatarUrl = loginInfo.Principal
    .FindFirst("picture")?.Value;
            _logger.LogInformation(
    "Google avatar claim exists: {HasAvatar}",
    !string.IsNullOrWhiteSpace(avatarUrl));
            var request = new ExternalLoginRequest
            {
                Provider = loginInfo.LoginProvider,
                ProviderKey = loginInfo.ProviderKey,
                Email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email),
                Name = loginInfo.Principal.FindFirstValue(ClaimTypes.Name),
                AvatarUrl = avatarUrl,
                Claims = loginInfo.Principal.Claims,
                RememberMe = rememberMe
            };

            var result = await _authService.ExternalLoginAsync(
                request,
                cancellationToken);

            switch (result.Status)
            {
                case ExternalLoginStatus.Authenticated:
                    {
                        if (result.AuthResponse == null ||
                            !result.AuthResponse.Success)
                        {
                            TempData["Error"] =
                                "Không thể hoàn tất đăng nhập.";

                            await HttpContext.SignOutAsync(
                                IdentityConstants.ExternalScheme);

                            return RedirectToAction(nameof(Login));
                        }

                        await SetAuthCookiesAsync(result.AuthResponse);

                        await HttpContext.SignOutAsync(
                            IdentityConstants.ExternalScheme);

                        var redirectUrl = Url.IsLocalUrl(returnUrl)
                            ? returnUrl!
                            : Url.Content("~/");

                        return Redirect(redirectUrl);
                    }

                case ExternalLoginStatus.EmailAlreadyExists:
                    {
                        TempData["Error"] = result.Message;

                        await HttpContext.SignOutAsync(
                            IdentityConstants.ExternalScheme);

                        return RedirectToAction(nameof(Login));
                    }

                case ExternalLoginStatus.RegistrationRequired:
                    {
                        // Không tạo auth cookie Caterin tại đây.
                        // Giữ External Cookie để trang hoàn tất đăng ký đọc lại loginInfo.
                        return RedirectToAction(
                            nameof(CompleteExternalRegistration),
                            new { returnUrl });
                    }

                default:
                    {
                        TempData["Error"] =
                            result.Message ??
                            "Đăng nhập bằng tài khoản ngoài thất bại.";

                        await HttpContext.SignOutAsync(
                            IdentityConstants.ExternalScheme);

                        return RedirectToAction(nameof(Login));
                    }
            }
        }

        [AllowAnonymous]
        [HttpGet("complete-external-registration")]
        [ResponseCache(
    Location = ResponseCacheLocation.None,
    NoStore = true)]
        public async Task<IActionResult> CompleteExternalRegistration(
    string? returnUrl)
        {
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();

            if (loginInfo == null)
            {
                TempData["Error"] =
                    "Phiên đăng ký đã hết hạn. Vui lòng đăng nhập lại.";

                return RedirectToAction(nameof(Login));
            }

            var supportedProviders = new[] { "Google", "Facebook" };

            if (!supportedProviders.Contains(
                    loginInfo.LoginProvider,
                    StringComparer.Ordinal))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] = "Nhà cung cấp đăng nhập không hợp lệ.";

                return RedirectToAction(nameof(Login));
            }

            var email = loginInfo.Principal.FindFirstValue(
                ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] =
                    "Không lấy được email từ tài khoản Google/Facebook.";

                return RedirectToAction(nameof(Login));
            }

            var model = new ExternalRegisterViewModel
            {
                Email = email,
                Provider = loginInfo.LoginProvider,
                UserName = string.Empty,
                AcceptTerms = false,
                AcceptPrivacy = false
            };

            model.ReturnUrl = Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost("complete-external-registration")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        [ResponseCache(
    Location = ResponseCacheLocation.None,
    NoStore = true)]
        public async Task<IActionResult> CompleteExternalRegistration(
    ExternalRegisterViewModel model,
    CancellationToken cancellationToken)
        {
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();

            if (loginInfo == null)
            {
                TempData["Error"] =
                    "Phiên đăng ký đã hết hạn. Vui lòng đăng nhập lại.";

                return RedirectToAction(nameof(Login));
            }

            // Không nhận Provider, ProviderKey và Email từ form.
            // Luôn lấy dữ liệu xác thực từ External Cookie.
            var supportedProviders = new[] { "Google", "Facebook" };

            if (!supportedProviders.Contains(
                    loginInfo.LoginProvider,
                    StringComparer.Ordinal))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] = "Nhà cung cấp đăng nhập không hợp lệ.";

                return RedirectToAction(nameof(Login));
            }

            var email = loginInfo.Principal.FindFirstValue(
                ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                TempData["Error"] = "Không lấy được email từ tài khoản ngoài.";

                return RedirectToAction(nameof(Login));
            }
            ModelState.Remove(nameof(model.Email));
            ModelState.Remove(nameof(model.Provider));
            // Chỉ nhận lại các trường hiển thị từ cookie.
            model.Email = email;
            model.Provider = loginInfo.LoginProvider;

            model.ReturnUrl = Url.IsLocalUrl(model.ReturnUrl)
                ? model.ReturnUrl
                : null;
            if (!model.AcceptTerms || !model.AcceptPrivacy)
            {
                ModelState.AddModelError(
                    nameof(model.AcceptTerms),
                    "Bạn phải đồng ý với Điều khoản sử dụng và Chính sách bảo mật.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new ExternalRegistrationRequest
            {
                Provider = loginInfo.LoginProvider,
                ProviderKey = loginInfo.ProviderKey,
                Email = email,
                UserName = model.UserName,
                Name = loginInfo.Principal.FindFirstValue(
                    ClaimTypes.Name),
                AvatarUrl = loginInfo.Principal.FindFirstValue("picture"),
                AcceptTerms = model.AcceptTerms,
                AcceptPrivacy = model.AcceptPrivacy
            };

            var result = await _authService.RegisterExternalAsync(
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.IsNullOrWhiteSpace(error.Field)
                            ? string.Empty
                            : error.Field,
                        error.Message);
                }

                return View(model);
            }

            // Đăng ký đã hoàn tất, không cần giữ External Cookie nữa.
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            TempData["Email"] = email;
            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(ConfirmEmail));
        }
        #endregion

        #region Helpers

        private void AddErrors(ServiceResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.ToString());
                _logger.LogError("Error: {error}", error.ToString());
            }
        }

        private Task SetAuthCookiesAsync(AuthResponse response)
        {

            Response.Cookies.Append("access_token", response.AccessToken!,
                CookieHelper.AccessToken(_jwtSettings.AccessTokenExpirationMinutes));

            Response.Cookies.Append("refresh_token", response.RefreshToken!,
                CookieHelper.RefreshToken(response.ExpireAt));

            return Task.CompletedTask;
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home",
                new { area = "Admin" });
        }

        #endregion Helpers
    }
}
