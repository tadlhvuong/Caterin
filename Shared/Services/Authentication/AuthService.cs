using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Auth;
using Shared.DTOs.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Responses;
using Shared.Services.Email;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System.Data;
using System.Text;

namespace Shared.Services.Authentication;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly AppSettings _appSettings;
    private readonly IUserService _userService;
    private readonly IAppCache _cache;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserValidationService _userValidationService;
    private readonly IEmailSender _emailSender;
    private readonly ISecurityLogger _securityLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IJwtService jwtService, IRefreshTokenService refreshTokenService,
        AppDbContext context,
        IOptions<JwtSetting> jwtSettings, 
        IOptions<AppSettings> appSettings,
        IAppCache appCache,
        IUserValidationService userValidationService,
        ICurrentUserService currentUserService,
        IEmailSender emailSender,
        ISecurityLogger securityLogger,
        ILogger<AuthService> logger, IUserService userService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _appSettings = appSettings.Value;
        _refreshTokenService = refreshTokenService;
        _cache = appCache;
        _userValidationService = userValidationService;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
        _securityLogger = securityLogger;
        _logger = logger;
        _userService = userService;
    }

    #region Login

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userService.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var validation = await _userValidationService.ValidateAsync(new()
        {
            Scenario = UserValidationScenario.Login,
            UserId = user.Id
        });

        if (!validation.Succeeded)
        {
            await _securityLogger.LogAsync(SecurityActionType.Login,false, "Invalid username or password.");
            _logger.LogWarning(validation.Error.ToString());
            
            return new AuthResponse
            {
                Success = false,
                Message = validation.Error.ToString()
            };
        }
        var roles = await _userService.GetRolesAsync(user.Id);

        return await GenerateAuthResponseAsync(user, roles);
    }

    #endregion

    #region Register

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return new AuthResponse
            {
                Success = false,
                Message = "Email not found"
            };
        var isEmailExists = await _userManager.Users.AnyAsync(x => x.Email == request.Email);

        if (isEmailExists)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        var isUsernameExists = await _userManager.Users.AnyAsync(x => x.UserName == request.UserName);

        if (isUsernameExists)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already exists"
            };
        }

        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await _userService.AssignRoleAsync(user.Id, "User");
        var roles = await _userService.GetRolesAsync(user.Id);
        return await GenerateAuthResponseAsync(user, roles);
    }

    #endregion

    #region Refresh Token

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // Load RefreshToken
        var refreshTokenEntity = await _refreshTokenService.GetByTokenAsync(refreshToken, cancellationToken);

        if (refreshTokenEntity?.User is null)
        {
            await _securityLogger.LogAsync(SecurityActionType.RefreshToken, false, "Refresh token invalid");
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token invalid"
            };
        }

        // Validate User
        var validation = await _userValidationService.ValidateAsync(
                new UserValidationContext
                {
                    Scenario = UserValidationScenario.RefreshToken,

                    UserId = refreshTokenEntity.UserId,

                    RefreshTokenEntity = refreshTokenEntity
                }, cancellationToken);

        if (!validation.Succeeded)
        {
            await _securityLogger.LogAsync(SecurityActionType.RefreshToken, false, validation.Error.ToString());
            return new AuthResponse
            {
                Success = false,
                Message = validation.Error.ToString()
            };
        }

        // Rotate
        var rotateResult = await _refreshTokenService.RotateAsync(refreshTokenEntity, cancellationToken);

        // Roles
        var roles = await _userManager.GetRolesAsync(rotateResult.User);

        // AccessToken
        var accessToken = _jwtService.GenerateAccessToken(rotateResult.User, roles);

        await _securityLogger.LogAsync(SecurityActionType.RefreshToken, true, "Refresh token successful");
        return new AuthResponse
        {
            Success = true,
            UserId = rotateResult.User.Id,
            Email = rotateResult.User.Email!,
            UserName = rotateResult.User.UserName!,
            AccessToken = accessToken,
            RefreshToken = rotateResult.RefreshToken,
            Message = "Refresh token successful"
        };
    }
    #endregion

    #region Logout

    public async Task LogoutAsync(string refreshToken,
    CancellationToken cancellationToken = default)
    {
        var refreshTokenEntity = await _refreshTokenService.GetByTokenAsync(refreshToken,cancellationToken);

        if (refreshTokenEntity == null)
        {
            return;
        }

        await _refreshTokenService.RevokeAllUserTokensAsync(refreshTokenEntity.UserId, cancellationToken);

        await _securityLogger.LogAsync(SecurityActionType.Logout, true, "Logout.");
        _logger.LogInformation("Logout success");
        await _refreshTokenService.RevokeAsync(refreshTokenEntity, cancellationToken);
    }

    #endregion

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request,
    CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync($"{request.Email}");

        if (user == null)
            return;

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var link =
            $"{_appSettings.WebsiteUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        //var link =
        //   $"https://caterin.vn/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        //await _emailSender.SendResetPasswordEmailAsync(
        //    user.Email!,
        //    user.UserName!,
        //    link);
        await _emailSender.SendEmailAsync(user.Email!, user.UserName!, link);
        await _securityLogger.LogAsync(SecurityActionType.ForgotPassword, true, "Reset password email sent.");
    }
    public async Task ResetPasswordByTokenAsync(ResetPasswordRequest request,
    CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync(request.Email);

        if (user == null)
            throw new Exception("User not found.");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _userService.IncreasePermissionVersionAsync(user.Id);
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        await _securityLogger.LogAsync(SecurityActionType.ResetPassword, true, "Password reset.");
    }
    public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request,
    CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();

        var user = await _userService.FindByIdAsync(userId);

        if (user == null)
            throw new Exception("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new Exception( string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        user.PermissionVersion++;
        await _userManager.UpdateAsync(user);
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id);
        await _securityLogger.LogAsync(SecurityActionType.ChangePassword, true, "Password changed.");
        return new ServiceResult
        {
            Success = true,
            Message = "Password changed successfully."
        };
    }
    #region Assign Role

    //public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    //{
    //    role = role.Trim();

    //    var user = await _userManager.FindByIdAsync(userId);

    //    if (user == null)
    //    {
    //        _logger.LogError("User not found");
    //        throw new Exception("User not found.");
    //    }

    //    if (!await _roleManager.RoleExistsAsync(role))
    //    {
    //        _logger.LogError($"Role '{role}' not found.");
    //        throw new Exception($"Role '{role}' not found.");
    //    }

    //    if (await _userManager.IsInRoleAsync(user, role))
    //    {
    //        _logger.LogError("Role user not found");
    //        return;
    //    }

    //    var addRoleResult = await _userManager.AddToRoleAsync(user, role);

    //    if (!addRoleResult.Succeeded)
    //    {
    //        _logger.LogError(string.Join(", ", addRoleResult.Errors.Select(x => x.Description)));
    //        throw new Exception(
    //            string.Join(", ", addRoleResult.Errors.Select(x => x.Description)));
    //    }
    //    await _userService.IncreasePermissionVersionAsync(user.Id);

    //    _logger.LogInformation("Assign role Changed.");
    //    await _securityLogger.LogAsync(SecurityActionType.RoleAssigned, true, "Role updated.");
    //}

    #endregion

    #region Validate Exists

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _userManager.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<bool> IsUsernameExistsAsync(
        string username)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == username);
    }

    #endregion

    #region Private Methods

    private async Task<AuthResponse> GenerateAuthResponseAsync(AppUser user, IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _jwtService.GenerateAccessToken(user, roles);

        var refreshToken = await _refreshTokenService.CreateAsync(user.Id, cancellationToken);

        await _securityLogger.LogAsync(SecurityActionType.Login, true, $"User {user.UserName} login success");
        _logger.LogInformation($"User {user.UserName} login success");
        return new AuthResponse
        {
            Success = true,
            UserId = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Message = "Login successful"
        };
    }

    #endregion
}
