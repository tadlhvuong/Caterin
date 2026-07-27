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
using Shared.Services.Email.EmailModels;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Website.Areas.Admin.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private readonly ILogger _logger;
        private readonly IActivityLogger _activityLogger;
        private readonly ISecurityLogger _securityLogger;

        public AuthController(IAuthService authService, SignInManager<AppUser> signInManager,
            AppDbContext dbContext, IOptions<JwtSetting> jwtSettings, ISecurityLogger securityLogger, 
            ILogger<AuthController> logger, IActivityLogger activityLogger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;

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
            if (ModelState.IsValid)
            {
                var result = await _authService.RegisterAsync(
                  new RegisterRequest
                  {
                      Email = model.Email,
                      Password = model.Password,
                      ConfirmPassword = model.ConfirmPassword,
                      UserName = model.UserName,
                  });

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                await _securityLogger.LogAsync(SecurityActionType.Login, true, "Login success");
                return RedirectToAction(nameof(ConfirmEmail), new
                {
                    email = model.Email
                });
            }
            return View(model);
        }

        [HttpGet("confirm-email")]
        public ActionResult ConfirmEmail()
        {
            _logger.LogInformation("Page: Verify Register Admin");
            return View();
        }

        [HttpGet("confirm-email/callback")]
        public async Task<IActionResult> ConfirmEmailCallback([FromQuery] string? userId, [FromQuery] string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Invalid confirmation link");
                ViewBag.ErrorMessage = "Invalid confirmation link.";
                return View("Error");
            }
            var result = await _authService.ConfirmEmailAsync(new ConfirmEmailRequest { UserId = userId, Token= token});
            if (!result.Succeeded)
            {
                ViewBag.ErrorMessage = string.Join("\n", result.Errors);
                return View("Error");
            }
            return View("ConfirmEmailSuccess");
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
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error forgot password: {0}", ex);
                    ViewBag.ErrorMessage = ex.Message;
                    return View("Error");
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
            _logger.LogInformation("Page: Forgotpassword confirmation");
            return View();////
        }

        #endregion forgot password

        #region reset password
        [HttpGet("reset-password")]
        public async Task<ActionResult> ResetPassword([FromQuery] string? userId, [FromQuery] string? token)
        {
            _logger.LogInformation("Page: Reset password");
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogError("reset passowrd error");
                return View(model);
            }

            if (model.UserId == null || model.Token == null)
            {
                _logger.LogError("reset password error not found user or code");
                ViewBag.ErrorMessage = "Đường dẫn không hợp lệ";
                ModelState.AddModelError(string.Empty, "Đường dẫn không hợp lệ");
                return View("Error");
            }
            var request = new ResetPasswordRequest {
                UserId = model.UserId,
                Token = model.Token,
                NewPassword = model.Password
            };
            var result = await _authService.ResetPasswordByTokenAsync(request);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet("reset-password-confirm")]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        #endregion reset password

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

            Response.Cookies.Delete("access_token");

            Response.Cookies.Delete("refresh_token");

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

            Response.Cookies.Delete("access_token");

            Response.Cookies.Delete("refresh_token");

            return RedirectToAction("Login");
        }

        #endregion Logout

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
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
        private void AddErrors(ServiceResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.ToString());
                _dbContext.SaveChanges();

                _logger.LogError("Error: {error}", error.ToString());
            }
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

                TempData["Error"] = "Đăng nhập Google/Facebook thất bại.";

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

        private Task SetAuthCookiesAsync(AuthResponse response)
        {

            Response.Cookies.Append("access_token", response.AccessToken!,
                CookieHelper.AccessToken(_jwtSettings.AccessTokenExpirationMinutes));

            Response.Cookies.Append("refresh_token", response.RefreshToken!,
                CookieHelper.RefreshToken(response.ExpireAt));

            return Task.CompletedTask;
        }

        //[HttpGet("external-login")]
        //public IActionResult ExternalLogin(LoginExViewModel model)
        //{
        //    var properties = _signInManager.ConfigureExternalAuthenticationProperties(model.Provider, "ExternalLoginCallbackUrl");
        //    return Challenge(properties, model.Provider);
        //}

        //[HttpGet("external-login-callback/{id?}")]
        //public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        //{
        //    if (remoteError != null)
        //    {
        //        _logger.LogWarning("Login Admin external fail remote");
        //        ModelState.AddModelError(string.Empty, $"Đăng nhập Facebook/Google lỗi: {remoteError}");
        //        return RedirectToAction(nameof(Login));
        //    }
        //    var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
        //    if (loginInfo == null)
        //    {
        //        _logger.LogWarning("User login ex not found");
        //        ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản đã đăng ký.");
        //        return RedirectToAction(nameof(Login));
        //    }
        //    var result = await _signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: false);
        //    if (result.Succeeded)
        //    {
        //        _logger.LogInformation("User login ex success.");
        //        var userId = (from x in _dbContext.UserLogins
        //                      where x.ProviderKey == loginInfo.ProviderKey
        //                      select x.UserId).SingleOrDefault();

        //        return (IActionResult)RedirectToAction(returnUrl);
        //    }
        //    if (result.RequiresTwoFactor)
        //    {
        //        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
        //    }
        //    if (result.IsLockedOut)
        //    {
        //        _logger.LogWarning("User locked 5 minitus.");
        //        ModelState.AddModelError(string.Empty, "Tài khoản bị tạm khóa. Vui lòng thử lại sau.");
        //    }
        //    else
        //    {
        //        var appUser = await CreateUserEx(loginInfo);
        //        if (appUser == null)
        //        {
        //            _logger.LogWarning("not create user ex");

        //            return View("ExternalLoginFailure", "Tạo tài khoản lỗi: " + loginInfo.ProviderDisplayName);
        //        }

        //        await _signInManager.SignInAsync(appUser, true);

        //        return (IActionResult)RedirectToLocal(returnUrl);
        //    }
        //    return View();
        //}

        //[HttpPost("confirm-external-login/{id?}")]
        //[AllowAnonymous, ValidateAntiForgeryToken]
        //public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        //{
        //    _logger.LogInformation("Login Admin external");
        //    if (_signInManager.IsSignedIn(User))
        //    {
        //        return RedirectToRoute("/admin/home");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        var info = await _signInManager.GetExternalLoginInfoAsync();
        //        if (info == null)
        //        {
        //            _logger.LogWarning("Login Admin external fail");
        //            return View("ExternalLoginFailure", "Login Admin external fail");
        //        }
        //        var user = new AppUser { UserName = CommonHelper.ConvertEmailToName(model.Email), Email = model.Email };
        //        var result = await _userManager.CreateAsync(user);

        //        var manageClaim = info.Principal.Claims.Where(c => c.Type == "ManageStore").FirstOrDefault();
        //        if (manageClaim != null)
        //        {
        //            await _userManager.AddClaimAsync(user, manageClaim);
        //        }

        //        if (result.Succeeded)
        //        {
        //            result = await _userManager.AddLoginAsync(user, info);
        //            if (result.Succeeded)
        //            {
        //                _logger.LogInformation("Login Admin external success");
        //                await _signInManager.SignInAsync(user, isPersistent: false);
        //                return (ActionResult)RedirectToLocal("Login");
        //            }
        //        }
        //        AddErrors(result);
        //    }

        //    ViewBag.ReturnUrl = returnUrl;
        //    return View(model);
        //}


        //private async Task<string> GetUserName(ExternalLoginInfo loginInfo)
        //{
        //    _logger.LogInformation("Get username");
        //    string defaultName = null;
        //    if (loginInfo.LoginProvider == "Facebook" || loginInfo.LoginProvider == "Google")
        //    {
        //        var nameClaim = loginInfo.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        //        if (nameClaim != null)
        //            defaultName = CommonHelper.NormalizeVietnamese(nameClaim.Value);
        //    }

        //    if (defaultName == null)
        //        return null;

        //    string newUserName = defaultName;
        //    for (int i = 0; i < 30; i++)
        //    {
        //        AppUser newUser = await _userManager.FindByNameAsync(newUserName);
        //        if (newUser == null)
        //            break;

        //        int randNo = CommonHelper.Random(99) + 1;
        //        newUserName = string.Format("{0}{1:D2}", defaultName, randNo);
        //    }

        //    return newUserName;
        //}
        //private async Task<AppUser> CreateUserEx(ExternalLoginInfo loginInfo)
        //{
        //    _logger.LogInformation("Create userEx");
        //    string newUserName = await GetUserName(loginInfo);
        //    if (newUserName == null)
        //        return null;

        //    var exEmail = loginInfo.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        //    AppUser appUser = new AppUser
        //    {
        //        UserName = newUserName,
        //        Email = exEmail?.Value,
        //        EmailConfirmed = (exEmail != null),
        //        CreatedAt = DateTime.Now,
        //        UpdatedAt = DateTime.Now,
        //        Status = EntityStatus.Enabled
        //    };

        //    var result = await _userManager.CreateAsync(appUser);
        //    if (!result.Succeeded)
        //    {
        //        return null;
        //    }

        //    result = await _userManager.AddLoginAsync(appUser, loginInfo);
        //    if (!result.Succeeded)
        //    {
        //        return null;
        //    }

        //    return appUser;
        //}
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction(nameof(HomeController.Index), "Home", new { area = "Admin" });

            return Redirect(returnUrl);
        }
        #endregion
    }
}
