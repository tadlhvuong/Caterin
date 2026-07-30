using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Notification;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Services.Email;
using Shared.Services.Email.EmailModels;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Website.Areas.Admin.Controllers;
using Website.Areas.Admin.Models;

namespace Website.Controllers
{
    [Route("account")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;
        SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;
        private readonly JwtSetting _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly ISecurityLogger _securityLogger;
        private readonly IEmailTemplateService _emailTemplate;
        private readonly IEmailSender _emailSender;

        public AccountController(IAuthService authService, ILogger<AuthController> logger,
        UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            AppDbContext dbContext, IOptions<JwtSetting> jwtSettings, ISecurityLogger securityLogger,
            IEmailTemplateService emailTemplate, IEmailSender emailSender)
        {
            _authService = authService;
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _securityLogger = securityLogger;
            _emailTemplate = emailTemplate;
            _emailSender = emailSender;
        }
        public ActionResult Index()
        {
            return RedirectToAction("login", "auth");
        }

        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("Page: Login Admin");
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(
                    new LoginRequest
                    {
                        Email = model.Email,
                        Password = model.Password
                    });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                return View(model);
            }

            Response.Cookies.Append("access_token", result.AccessToken!, CookieHelper.AccessToken(_jwtSettings.AccessTokenExpirationMinutes));

            DateTime expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            Response.Cookies.Append("refresh_token", result.RefreshToken!,
                CookieHelper.RefreshToken(expiresAt));

            await _securityLogger.LogAsync(SecurityActionType.Login, true, "Login success");
            if (!string.IsNullOrWhiteSpace(
                    model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: AccountController/Details/5
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var checkUser = await _userManager.FindByEmailAsync(model.Email);

            if (checkUser != null)
            {
                ModelState.AddModelError(nameof(model.Email),"Trùng tài khoản");
                return View(model);
            }
            if (ModelState.IsValid)
            {
                var user = new AppUser { UserName = model.UserName, Email = model.Email, CreatedAt = DateTime.Now };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("{0}: User was created.", user.UserName);
                    string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var callbackUrl = Url.Action("ConfirmEmailCallback", "Auth", new { userId = user.Id, code = encodedToken }, protocol: HttpContext.Request.Scheme);
                    var html = await _emailTemplate.RenderAsync("ConfirmEmail",
                    new ConfirmEmailModel
                    {
                        UserName = user.UserName,
                        ConfirmEmailUrl = callbackUrl,
                        ExpireHours = 5,
                    });
                    await _emailSender.SendEmailAsync( user.Email!, "Xác nhận Email", html);
                    await _dbContext.SaveChangesAsync();

                   return Redirect("/admin/auth/confirm-register");
                }
                AddErrors(result);
            }
            return View(model);
        }

        [HttpGet("confirm-register")]
        public ActionResult RegisterConfirmation()
        {
            _logger.LogInformation("Page: Verify Register Admin");
            return View();
        }

        [HttpPost("confirm-email/{id?}")]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.LogError("register comfirm error not found user or code");
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("register comfirm error not found user");
                return View("Error");
            }
            user.Status = EntityStatus.Active;
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                _logger.LogInformation("register comfirm success");
                return View("ConfirmEmail");
            }
            else
            {
                ViewBag.ErrorMessage = string.Join("\n", result.Errors);
                return View("Error");
            }
        }

        [HttpGet("account/forgot-password")]
        public ActionResult ForgotPassword()
        {
            _logger.LogInformation("Page: Forgot password");
            return View();
        }

        [HttpPost("account/forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        if (user == null)
                        {
                            _logger.LogWarning("{0}: Email not found", model.Email);
                            ModelState.AddModelError(string.Empty, "Không tìm thấy email hợp lệ" );

                        }
                        else
                        {
                            _logger.LogWarning("{0}:Your email address has not been verified. ", model.Email);
                            ModelState.AddModelError(string.Empty, "Email chưa xác thực");
                        }
                        return View(model);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(user.PasswordHash))
                        {
                            _logger.LogWarning("Account login Facebook/Google+ don't request reset password", model.Email);
                             await _dbContext.SaveChangesAsync();

                            ModelState.AddModelError(string.Empty, "Account login Facebook/Google+ don't request reset password");
                            return View(model);
                        }
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = HttpUtility.UrlEncode(token);
                    var callbackUrl = Url.Action("reset-password", "Account", new { userId = user.Id, code = encodedToken }, protocol: HttpContext.Request.Scheme);
                    var html = await _emailTemplate.RenderAsync( "ResetPassword",
                                new ResetPasswordEmailModel
                                {
                                    UserName = user.UserName!,
                                    ResetPasswordUrl = callbackUrl,
                                    ExpireMinutes = 5,
                                    SiteName = "HKProject",
                                    LogoUrl = "https://...",
                                    SupportPhone = "0903 653 303"
                                });

                    await _emailSender.SendEmailAsync(user.Email!, "Đặt lại mật khẩu", html);
                    await _dbContext.SaveChangesAsync();

                    return Redirect("/admin/account/confirm-forgot-password");
                    
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

        [HttpPost("account/confirm-forgot-password")]
        public ActionResult ForgotPasswordConfirmation()
        {
            _logger.LogInformation("Page: Forgotpassword confirmation");
            return View();
        }

        [HttpPost("account/reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(string key)
        {
            _logger.LogInformation("Page: Reset password");
            if (key == null )
            {
                _logger.LogError("reset password error not found user or code");
                ViewBag.ErrorMessage = "Đường dẫn không hợp lệ";
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(key);
            if (user == null)
            {
                _logger.LogError("reset password error not found user");
                ViewBag.ErrorMessage = "Không tìm thấy tài khoản hợp lệ";
                return View("Error");
            }
            var resetPasswordViewModel = new ResetPasswordViewModel() { Key = key };

            await _dbContext.SaveChangesAsync();

            if (key == null)
            {
                ViewBag.ErrorMessage = "Token invalid";
                return View("Error");
            }
            else
                return View(resetPasswordViewModel);

        }

        [Route("admin/en/account/reset-password")]
        [Route("admin/vi/tai-khoan/doi-mat-khau")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogError("reset passowrd error");
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.Key);
            if (user == null)
            {
                _logger.LogError("reset passowrd error not found user");

                return RedirectToAction("ResetPasswordConfirmationFailed", "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Key, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("reset passowrd success");
                
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            _logger.LogError("reset password error");
            return View();
        }
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        public ActionResult ResetPasswordConfirmationFailed()
        {
            return View();
        }
        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                //revoke refresh token
                await _authService.LogoutAsync(refreshToken);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            //delete access token
            Response.Cookies.Delete("access_token");
            //delete refresh token
            Response.Cookies.Delete("refresh_token");

            return RedirectToAction("Login");
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
                _dbContext.SaveChanges();

                _logger.LogError("Error: {error}", error.Description);
            }
        }
    }
}
