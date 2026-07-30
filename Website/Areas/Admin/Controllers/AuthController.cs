using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.Data.Entities.Notification;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Responses;
using Shared.Services.Email;
using System.Security.Claims;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("[area]/auth")]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _dbContext;
        private readonly JwtSetting _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly IEmailActionService _emailActionService;

        private readonly ILogger _logger;
        private readonly IActivityLogger _activityLogger;
        private readonly ISecurityLogger _securityLogger;

        public AuthController(IAuthService authService, SignInManager<AppUser> signInManager,
            AppDbContext dbContext, IOptions<JwtSetting> jwtSettings, ISecurityLogger securityLogger, 
            ILogger<AuthController> logger, IActivityLogger activityLogger, IEmailActionService emailActionService)
        {
            _authService = authService;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _emailActionService = emailActionService;

            _activityLogger = activityLogger;
            _logger = logger;
            _securityLogger = securityLogger;
        }
        public ActionResult Index()
        {
            return RedirectToAction("login", "auth");
        }

        #region login
        [HttpGet("login")]
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

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
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

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            _logger.LogInformation(result.UserName + ": login success");
            await _securityLogger.LogAsync(SecurityActionType.Login, true, "Login success");
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        #endregion login

        #region regsiter

        [HttpGet("register")]
        public IActionResult Register()
        {
            _logger.LogInformation("Page: Register Admin");
            return View();
        }   

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
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

            await _securityLogger.LogAsync(SecurityActionType.Login, true, "Login success");
            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(ConfirmEmail));
        }

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

        [HttpPost("resend-confirm-email")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmEmail(
    ResendConfirmEmailViewModel model,
    CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(nameof(ConfirmEmail), model);

            var result = await _authService.ResendConfirmEmailAsync(
                model.Email,
                cancellationToken);

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
            //return View("Feedback", new FeedbackViewModel
            //{
            //    Type = FeedbackType.Success,
            //    Title = "Gửi lại email xác thực",
            //    Message = "Nếu email tồn tại trong hệ thống và chưa được xác thực, chúng tôi đã gửi email xác thực.",
            //});
        }

        #endregion register

        #region forgot password
        [HttpGet("forgot-password")]
        public ActionResult ForgotPassword()
        {
            _logger.LogInformation("Page: Forgot password");
            return View();
        }

        [HttpPost("forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _authService.ForgotPasswordAsync(
                        new ForgotPasswordRequest
                        {
                            Email = model.Email
                        });

                    if (!result.Succeeded)
                    {
                        AddErrors(result);
                        return View(model);
                    }
                    TempData["Feedback"] = true;
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error forgot password: {0}", ex);
                    return View("Feedback", new FeedbackViewModel
                    {
                        Type = FeedbackType.Danger,
                        Title = "Đã xảy ra lỗi",
                        Message = "Không thể xử lý yêu cầu của bạn. Vui lòng thử lại sau."
                    });
                }
            }
            else
            {
                _logger.LogWarning("Email: this field is required");
            }
            return View(model);
        }

        [HttpGet("confirm-forgot-password")]
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
        [HttpGet("reset-password")]
        public IActionResult ResetPassword([FromQuery] string? key)
        {
            _logger.LogInformation("Page: Reset password");
            if (string.IsNullOrWhiteSpace(key))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                Key = key
            });
        }

        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            _logger.LogInformation("Reset password");
            if (!ModelState.IsValid)
            {
                _logger.LogError("Đổi mật khẩu lỗi");
                return View(model);
            }

            if (model.Key == null)
            {
                _logger.LogError("Liên kết đặt lại mật khẩu không hợp lệ.");
                ModelState.AddModelError(string.Empty, "Liên kết đặt lại mật khẩu không hợp lệ.");
                return View(model);
            }
            var request = new ResetPasswordRequest {
                Key = model.Key,
                NewPassword = model.Password
            };
            var result = await _authService.ResetPasswordByTokenAsync(request);
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

        [HttpGet("reset-password-confirm")]
        public ActionResult ResetPasswordConfirmation()
        {
            if (TempData["ResetPasswordSuccess"] is not true)
            {
                return RedirectToAction(nameof(Login));
            }
            return View("Feedback", new FeedbackViewModel
            {
                Type = FeedbackType.Success,
                Title = "Đặt lại mật khẩu",
                Message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập bằng mật khẩu mới."
            });
        }

        #endregion reset password

        #region Change password

        [HttpGet("change-password")]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost("change-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.Password,
                ConfirmPassword = model.ConfirmPassword
            };

            var result = await _authService.ChangePasswordAsync(request);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.SignOutAsync();
            ClearAuthCookies();

            return RedirectToAction(nameof(ChangePasswordConfirmation));
        }
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

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ClearAuthCookies();

            return RedirectToAction("Login");
        }

        [HttpPost("logout-all")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> LogoutAll()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            await HttpContext.SignOutAsync();
            ClearAuthCookies();

            return RedirectToAction("Login");
        }

        #endregion Logout

        [HttpGet("e/{key}")]
        public async Task<IActionResult> EmailAction(string key,
    CancellationToken cancellationToken)
        {
            var checkValid = await _emailActionService.GetValidAsync(key, cancellationToken);
            if (!checkValid.Succeeded)
            {
                return View("Feedback", new FeedbackViewModel
                {
                    Type = FeedbackType.Danger,
                    Title = "Liên kết không hợp lệ",
                    Message = string.Join("<br/>", checkValid.Errors)
                });
            }
            var emailAction = checkValid.Data!;
            switch (emailAction.Type)
            {
                case EmailActionType.ConfirmEmail:
                    {
                        var result = await _authService.ConfirmEmailAsync(emailAction);

                        if (!result.Succeeded)
                        {
                            return View("Feedback", new FeedbackViewModel
                            {
                                Type = FeedbackType.Danger,
                                Title = "Xác thực email thất bại",
                                Message = string.Join("<br/>", result.Errors)
                            });
                        }

                        return View("Feedback", new FeedbackViewModel
                        {
                            Title = "Xác thực email",
                            Message = "Bạn đã xác thực email thành công."
                        });
                    }

                case EmailActionType.ResetPassword:
                    {
                        return RedirectToAction(nameof(ResetPassword), new { key });
                    }
                default:
                    return View("Feedback", new FeedbackViewModel
                    {
                        Type = FeedbackType.Danger,
                        Title = "Liên kết không hợp lệ",
                        Message = "Loại liên kết không được hỗ trợ."
                    });
            }
        }

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

        [HttpPost("save-permission")]
        public async Task<IActionResult> SavePermission(string roleId, List<int> permissionIds)
        {
            var old = _dbContext.RolePermissions
                .Where(x => x.RoleId == roleId);

            _dbContext.RolePermissions.RemoveRange(old);

            _dbContext.RolePermissions.AddRange(
                permissionIds.Select(id => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = id
                }));

            await _dbContext.SaveChangesAsync();

            _cache.Remove($"perm:role:{roleId}");

            return View();
        }

        #region LoginEx

        [HttpPost("external-login")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(LoginExViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Provider))
            {
                return RedirectToAction(nameof(Login));
            }
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth",
                new { area = "Admin", returnUrl = model.ReturnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                model.Provider, redirectUrl);
            properties.IsPersistent = model.RememberMe;
           
            return Challenge(properties, model.Provider);
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl, 
            string? remoteError = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                _logger.LogWarning("External login failed: {Error}", remoteError);

                TempData["Error"] = "Đăng nhập bằng Google/Facebook thất bại. Vui lòng thử lại.";

                return RedirectToAction(nameof(Login));
            }

            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            bool rememberMe = loginInfo?.AuthenticationProperties?.IsPersistent ?? false;
            if (loginInfo == null)
            {
                _logger.LogWarning("ExternalLoginInfo not found.");

                TempData["Error"] = "Không lấy được thông tin đăng nhập.";

                return RedirectToAction(nameof(Login));
            }
            var request = new ExternalLoginRequest
            {
                Provider = loginInfo.LoginProvider,
                ProviderKey = loginInfo.ProviderKey,
                Email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email),
                Name = loginInfo.Principal.FindFirstValue(ClaimTypes.Name),
                Claims = loginInfo.Principal.Claims,
                RememberMe = rememberMe,
                AccessToken = loginInfo.AuthenticationTokens?.FirstOrDefault(x => x.Name == "access_token")?.Value,
                RefreshToken = loginInfo.AuthenticationTokens?.FirstOrDefault(x => x.Name == "refresh_token")?.Value
            };

            var result = await _authService.ExternalLoginAsync(request, cancellationToken);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;

                return RedirectToAction(nameof(Login));
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                TempData["Error"] = "Không thể lấy địa chỉ email từ tài khoản Google/Facebook.";
                return RedirectToAction(nameof(Login));
            }

            await SetAuthCookiesAsync(result);
            return Redirect(returnUrl ?? "/");
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

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction(nameof(HomeController.Index), "Home", new { area = "Admin" });

            return Redirect(returnUrl);
        }

        #endregion Helpers
    }
}
