using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

using Shared.Common;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Requests;
using Shared.Requests.Auth;
using Shared.Responses;
using Shared.Services.Email;
using System.Security.Claims;
using System.Text.RegularExpressions;

using Website.Models.Account;

namespace Website.Controllers
{
    [Route("account")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private const string AccessTokenCookie = "access_token";
        private const string RefreshTokenCookie = "refresh_token";

        private readonly IAuthService _authService;
        private readonly IEmailActionService _emailActionService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly JwtSetting _jwtSettings;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            IEmailActionService emailActionService,
            SignInManager<AppUser> signInManager,
            IOptions<JwtSetting> jwtSettings,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _emailActionService = emailActionService;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        #region SEO

        private void SetAuthSeo(string title, string description)
        {
            ViewData["Title"] = title;
            ViewData["SeoTitle"] = $"{title} | Caterin";
            ViewData["SeoDescription"] = description;
            ViewData["SeoRobots"] = "noindex, follow";

            var action = ControllerContext.RouteData.Values["action"]?.ToString()
                         ?? "Login";

            ViewData["SeoCanonical"] = Url.Action(
                action,
                "Account",
                values: null,
                protocol: Request.Scheme);
        }

        #endregion

        #region Login

        [HttpGet("login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            SetAuthSeo(
                "Đăng nhập",
                "Đăng nhập tài khoản Caterin để quản lý thông tin và trải nghiệm mua sắm.");

            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel
            {
                ReturnUrl = IsLocalReturnUrl(returnUrl) ? returnUrl : null
            });
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Đăng nhập",
                "Đăng nhập tài khoản Caterin để quản lý thông tin và trải nghiệm mua sắm.");

            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(
                new LoginRequest
                {
                    Email = model.Email.Trim(),
                    Password = model.Password,
                    IsRemember = model.RememberMe
                },
                cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message ?? "Đăng nhập không thành công.");

                return View(model);
            }

            SetAuthCookies(result);

            return RedirectAfterAuthentication(model.ReturnUrl);
        }

        #endregion

        #region Register

        [HttpGet("register")]
        public async Task<IActionResult> Register()
        {
            SetAuthSeo(
                "Đăng ký tài khoản",
                "Tạo tài khoản Caterin để sử dụng các tiện ích và quản lý đơn hàng.");

            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Đăng ký tài khoản",
                "Tạo tài khoản Caterin để sử dụng các tiện ích và quản lý đơn hàng.");

            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(
                new RegisterRequest
                {
                    Email = model.Email.Trim(),
                    UserName = model.UserName.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim(),
                    Password = model.Password,
                    AcceptTerms = model.AcceptTerms
                },
                cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message ?? "Đăng ký không thành công.");

                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        [HttpGet("register/confirmation")]
        public async Task<IActionResult> RegisterConfirmation()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            SetAuthSeo(
                "Kiểm tra email",
                "Vui lòng kiểm tra email để hoàn tất đăng ký tài khoản Caterin.");

            return View("RegisterConfirmation");
        }

        #endregion

        #region Confirm Email

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(
            string? key,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Xác thực email",
                "Xác thực địa chỉ email để kích hoạt tài khoản Caterin.");

            if (string.IsNullOrWhiteSpace(key))
            {
                ViewBag.ErrorMessage =
                    "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";

                return View("ConfirmEmailFailed");
            }

            var actionResult = await _emailActionService.GetValidAsync(
                key,
                EmailActionType.ConfirmEmail,
                cancellationToken);

            if (!actionResult.Succeeded || actionResult.Data == null)
            {
                ViewBag.ErrorMessage =
                    "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";

                return View("ConfirmEmailFailed");
            }

            var result = await _authService.ConfirmEmailAsync(
                actionResult.Data,
                cancellationToken);

            if (!result.Succeeded)
            {
                ViewBag.ErrorMessage = result.Message;

                return View("ConfirmEmailFailed");
            }

            return View("ConfirmEmail");
        }

        [HttpGet("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmEmail()
        {
            SetAuthSeo(
                "Gửi lại email xác thực",
                "Yêu cầu gửi lại email xác thực tài khoản Caterin.");

            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            return View("ResendConfirmEmail",
                new ResendConfirmationViewModel());
        }

        [HttpPost("resend-confirm-email")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmEmail(
            ResendConfirmationViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Gửi lại email xác thực",
                "Yêu cầu gửi lại email xác thực tài khoản Caterin.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
                return View("ResendConfirmEmail", model);

            var result = await _authService.ResendConfirmEmailAsync(
                model.Email.Trim(),
                cancellationToken);

            // Thông báo chung, không tiết lộ email có tồn tại hay không.
            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(ResendConfirmEmail));
        }

        #endregion

        #region Forgot Password

        [HttpGet("forgot-password")]
        public async Task<IActionResult> ForgotPassword()
        {
            SetAuthSeo(
                "Quên mật khẩu",
                "Nhận hướng dẫn đặt lại mật khẩu tài khoản Caterin qua email.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost("forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Quên mật khẩu",
                "Nhận hướng dẫn đặt lại mật khẩu tài khoản Caterin qua email.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.ForgotPasswordAsync(
                new ForgotPasswordRequest
                {
                    Email = model.Email.Trim()
                },
                cancellationToken);

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet("forgot-password/confirmation")]
        public async Task<IActionResult> ForgotPasswordConfirmation()
        {
            SetAuthSeo(
                "Kiểm tra email",
                "Hướng dẫn đặt lại mật khẩu đã được xử lý.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            return View("ForgotPasswordConfirmation");
        }

        #endregion

        #region Reset Password

        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string? key)
        {
            SetAuthSeo(
                "Đặt lại mật khẩu",
                "Tạo mật khẩu mới cho tài khoản Caterin.");
           
            if (string.IsNullOrWhiteSpace(key))
            {
                ViewBag.ErrorMessage =
                    "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";

                return View("ResetPasswordFailed");
            }

            return View(new ResetPasswordViewModel
            {
                Key = key
            });
        }

        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Đặt lại mật khẩu",
                "Tạo mật khẩu mới cho tài khoản Caterin.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.ResetPasswordByTokenAsync(
                model.Key,
                new ResetPasswordRequest
                {
                    NewPassword = model.Password
                },
                cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message ?? "Không thể đặt lại mật khẩu.");

                return View(model);
            }

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet("reset-password/confirmation")]
        public async Task<IActionResult> ResetPasswordConfirmation()
        {
            SetAuthSeo(
                "Đặt lại mật khẩu thành công",
                "Mật khẩu tài khoản Caterin đã được cập nhật.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            return View("ResetPasswordConfirmation");
        }

        [HttpGet("reset-password/failed")]
        public async Task<IActionResult> ResetPasswordFailed()
        {
            SetAuthSeo(
                "Không thể đặt lại mật khẩu",
                "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            return View("ResetPasswordFailed");
        }

        #endregion

        #region External Login

        [HttpPost("external-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(
    string provider,
    string? returnUrl = null,
    bool rememberMe = false)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest();

            provider = provider.Trim();

            if (provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                provider = "Google";
            }
            else if (provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
            {
                provider = "Facebook";
            }
            else
            {
                return BadRequest();
            }

            var safeReturnUrl = IsLocalReturnUrl(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Home");

            var callbackUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new { returnUrl = safeReturnUrl },
                Request.Scheme);

            // Làm giống AuthController.
            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(
                    provider,
                    callbackUrl!);

            properties.IsPersistent = false;
            properties.Items["RememberMe"] = rememberMe.ToString();

            return Challenge(properties, provider);
        }

        [HttpGet("external-login/callback")]
        public async Task<IActionResult> ExternalLoginCallback(
            string? returnUrl = null,
            string? remoteError = null,
            CancellationToken cancellationToken = default)
        {
            SetAuthSeo(
                "Đăng nhập",
                "Đăng nhập tài khoản Caterin.");
            //if (User.Identity?.IsAuthenticated == true)
            //{
            //    await ClearExternalCookieAsync();
            //    return RedirectToAction("Index", "Home");
            //}
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                await ClearExternalCookieAsync();

                TempData["ErrorMessage"] =
                    "Không thể đăng nhập bằng tài khoản bên ngoài. Vui lòng thử lại.";

                return RedirectToAction(nameof(Login));
            }

            // Đọc External Authentication Cookie.
            var externalInfo =
                await _signInManager.GetExternalLoginInfoAsync();

            if (externalInfo == null)
            {
                await ClearExternalCookieAsync();

                TempData["ErrorMessage"] =
                    "Không lấy được thông tin xác thực từ nhà cung cấp.";

                return RedirectToAction(nameof(Login));
            }

            var provider = externalInfo.LoginProvider;

            if (!IsSupportedProvider(provider))
            {
                await ClearExternalCookieAsync();

                TempData["ErrorMessage"] =
                    "Nhà cung cấp đăng nhập không được hỗ trợ.";

                return RedirectToAction(nameof(Login));
            }

            var principal = externalInfo.Principal;

            var email = principal.FindFirstValue(ClaimTypes.Email);

            var name = principal.FindFirstValue(ClaimTypes.Name)
                       ?? principal.FindFirstValue("name");

            var avatarUrl = principal.FindFirstValue("picture")
                            ?? principal.FindFirstValue("urn:google:picture");

            // Lấy RememberMe từ AuthenticationProperties của OAuth.
            var authenticationResult = await HttpContext.AuthenticateAsync(
                IdentityConstants.ExternalScheme);

            var rememberMe = false;

            if (authenticationResult.Properties?.Items.TryGetValue(
                    "RememberMe",
                    out var rememberValue) == true)
            {
                bool.TryParse(rememberValue, out rememberMe);
            }

            var result = await _authService.ExternalLoginAsync(
                new ExternalLoginRequest
                {
                    Provider = provider,
                    ProviderKey = externalInfo.ProviderKey,
                    Email = email,
                    Name = name,
                    AvatarUrl = avatarUrl,
                    RememberMe = rememberMe
                },
                cancellationToken);

            switch (result.Status)
            {
                case ExternalLoginStatus.Authenticated:

                    if (result.AuthResponse == null ||
                        !result.AuthResponse.Success)
                    {
                        await ClearExternalCookieAsync();

                        TempData["ErrorMessage"] =
                            "Không thể hoàn tất đăng nhập.";

                        return RedirectToAction(nameof(Login));
                    }

                    SetAuthCookies(result.AuthResponse);

                    await ClearExternalCookieAsync();

                    return RedirectAfterAuthentication(returnUrl);

                case ExternalLoginStatus.EmailAlreadyExists:

                    await ClearExternalCookieAsync();

                    TempData["ErrorMessage"] =
                        result.Message ??
                        "Email này đã được sử dụng. Vui lòng đăng nhập bằng tài khoản Caterin hiện có.";

                    return RedirectToAction(nameof(Login));

                case ExternalLoginStatus.RegistrationRequired:

                    // Quan trọng: Không xóa External Cookie.
                    // ExternalRegister sẽ đọc lại thông tin từ cookie này.
                    return RedirectToAction(nameof(ExternalRegister), new { returnUrl });
                default:

                    await ClearExternalCookieAsync();

                    TempData["ErrorMessage"] =
                        result.Message ?? "Đăng nhập ngoài thất bại.";

                    return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet("external-register")]
        public async Task<IActionResult> ExternalRegister()
        {
            SetAuthSeo(
                "Hoàn tất đăng ký",
                "Hoàn tất tạo tài khoản Caterin bằng Google hoặc Facebook.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null || !IsSupportedProvider(info.LoginProvider))
            {
                await ClearExternalCookieAsync();

                TempData["ErrorMessage"] =
                    "Thông tin đăng ký đã hết hạn. Vui lòng đăng nhập lại.";

                return RedirectToAction(nameof(Login));
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            var model = new ExternalRegisterViewModel
            {
                Email = email,
                UserName = !string.IsNullOrWhiteSpace(email)
                    ? await GenerateUserNameAsync(email)
                    : null
            };

            return View("ExternalRegister", model);
        }

        [HttpPost("external-register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalRegister(
            ExternalRegisterViewModel model,
            CancellationToken cancellationToken)
        {
            SetAuthSeo(
                "Hoàn tất đăng ký",
                "Hoàn tất tạo tài khoản Caterin bằng Google hoặc Facebook.");
            if (User.Identity?.IsAuthenticated == true)
            {
                await ClearExternalCookieAsync();
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
                return View("ExternalRegister", model);

            // Luôn đọc lại thông tin từ External Authentication Cookie.
            // Không nhận ProviderKey từ hidden input hoặc form.
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null || !IsSupportedProvider(info.LoginProvider))
            {
                await ClearExternalCookieAsync();

                TempData["ErrorMessage"] =
                    "Thông tin đăng ký đã hết hạn. Vui lòng đăng nhập lại.";

                return RedirectToAction(nameof(Login));
            }

            var principal = info.Principal;

            var provider = info.LoginProvider;
            var providerKey = info.ProviderKey;

            var email = principal.FindFirstValue(ClaimTypes.Email);

            // Chỉ yêu cầu nhập email nếu provider không cung cấp email.
            email = string.IsNullOrWhiteSpace(email)
                ? model.Email?.Trim()
                : email.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Vui lòng nhập email để đăng ký.");

                return View("ExternalRegister", model);
            }

            var name = principal.FindFirstValue(ClaimTypes.Name)
                       ?? principal.FindFirstValue("name");

            var avatarUrl = principal.FindFirstValue("picture")
                            ?? principal.FindFirstValue("urn:google:picture");

            var userName = string.IsNullOrWhiteSpace(model.UserName)
                ? await GenerateUserNameAsync(email)
                : model.UserName.Trim();

            var result = await _authService.RegisterExternalAsync(
                new ExternalRegistrationRequest
                {
                    Provider = provider,
                    ProviderKey = providerKey,
                    Email = email,
                    UserName = userName,
                    Name = name,
                    AcceptTerms = model.AcceptTerms,
                    AcceptPrivacy = model.AcceptPrivacy
                },
                cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message ?? "Không thể hoàn tất đăng ký.");

                return View("ExternalRegister", model);
            }

            // Chỉ xóa External Cookie sau khi đăng ký thành công.
            await ClearExternalCookieAsync();

            TempData["SuccessMessage"] = result.Message;

            // Không đăng nhập ngay. Tài khoản mới vẫn Pending.
            return RedirectToAction(nameof(RegisterConfirmation));
        }

        #endregion

        #region Logout

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        [ResponseCache(
    Location = ResponseCacheLocation.None,
    NoStore = true)]
        public async Task<IActionResult> Logout(
    CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookie];

            try
            {
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    await _authService.LogoutAsync(
                        refreshToken,
                        cancellationToken);
                }

                return Json(new
                {
                    success = true,
                    returnUrl = Url.Action(nameof(Login), "Account")
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to revoke session during logout.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Không thể hoàn tất đăng xuất. Vui lòng thử lại."
                    });
            }
            finally
            {
                DeleteAuthCookies();

                try
                {
                    await ClearExternalCookieAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to clear external authentication cookie.");
                }
            }
        }

        #endregion

        #region Access Denied

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            SetAuthSeo(
                "Không có quyền truy cập",
                "Bạn không có quyền truy cập trang này.");

            return View();
        }

        #endregion

        #region Helpers

        private bool IsLocalReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl)
                   && Url.IsLocalUrl(returnUrl);
        }

        private IActionResult RedirectAfterAuthentication(string? returnUrl)
        {
            return IsLocalReturnUrl(returnUrl)
                ? LocalRedirect(returnUrl!)
                : RedirectToAction("Index", "Home");
        }

        private static bool IsSupportedProvider(string? provider)
        {
            return provider != null &&
                   (provider.Equals("Google", StringComparison.OrdinalIgnoreCase) ||
                    provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase));
        }

        private void SetAuthCookies(AuthResponse result)
        {
            if (!result.Success ||
                string.IsNullOrWhiteSpace(result.AccessToken) ||
                string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Authentication response does not contain valid tokens.");
            }

            Response.Cookies.Append(
                AccessTokenCookie,
                result.AccessToken,
                CookieHelper.AccessToken(
                    _jwtSettings.AccessTokenExpirationMinutes));

            Response.Cookies.Append(
                RefreshTokenCookie,
                result.RefreshToken,
                CookieHelper.RefreshToken(result.ExpireAt));
        }

        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete(
                AccessTokenCookie,
                CookieHelper.Delete());

            Response.Cookies.Delete(
                RefreshTokenCookie,
                CookieHelper.Delete());
        }

        private async Task ClearExternalCookieAsync()
        {
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);
        }

        private async Task<string> GenerateUserNameAsync(string email)
        {
            var source = email.Split('@')[0];

            source = CommonHelper.NormalizeVietnamese(source)
                .ToLowerInvariant()
                .Replace(" ", "");

            source = Regex.Replace(source, @"[^a-z0-9_]", "");

            if (string.IsNullOrWhiteSpace(source))
                source = "user";

            var candidate = source;
            var index = 1;

            while (await _authService.IsUsernameExistsAsync(candidate))
            {
                candidate = $"{source}{index++}";
            }

            return candidate;
        }

        #endregion
    }
}