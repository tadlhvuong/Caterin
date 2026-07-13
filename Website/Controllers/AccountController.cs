using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Website.Areas.Admin.Controllers;
using Website.Areas.Admin.Models;

namespace Website.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;
        SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _dbContext;
        private readonly JwtSetting _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly ISecurityLogger _securityLogger;

        public AccountController(IAuthService authService, ILogger<AuthController> logger, SignInManager<AppUser> signInManager,
            AppDbContext dbContext, IOptions<JwtSetting> jwtSettings, ISecurityLogger securityLogger)
        {
            _authService = authService;
            _logger = logger;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings.Value;
            _securityLogger = securityLogger;
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

            Response.Cookies.Append("refresh_token", result.RefreshToken!,
                CookieHelper.RefreshToken(_jwtSettings.RefreshTokenExpirationDays));

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
        public ActionResult Register(int id)
        {
            return View();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

    }
}
