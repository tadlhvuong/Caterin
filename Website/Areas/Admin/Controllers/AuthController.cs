using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("[area]/auth")]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;
        SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _dbContext;
        private readonly JwtSetting _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly ISecurityLogger _securityLogger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, SignInManager<AppUser> signInManager,
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

            if (!string.IsNullOrWhiteSpace(
                    model.ReturnUrl))
            {
                return LocalRedirect(
                    model.ReturnUrl);
            }
            await _securityLogger.LogAsync(SecurityActionType.Login, true, "Login success");
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var refreshToken =
                Request.Cookies[
                    "refresh_token"];

            if (!string.IsNullOrWhiteSpace(
                    refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            Response.Cookies.Delete(
                "access_token");

            Response.Cookies.Delete(
                "refresh_token");

            return RedirectToAction("Login");
        }

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

    }
}
