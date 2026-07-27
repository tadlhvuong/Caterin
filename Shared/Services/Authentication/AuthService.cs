using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
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
using Shared.Services.Email.EmailModels;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Shared.Services.Authentication;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly JwtSetting _jwtSettings;
    private readonly IUserService _userService;
    private readonly IAppCache _cache;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserValidationService _userValidationService;
    private readonly IEmailSender _emailSender;
    private readonly ISecurityLogger _securityLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailTemplateService _emailTemplate;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IJwtService jwtService, IOptions<JwtSetting> jwtOptions, IRefreshTokenService refreshTokenService,
        AppDbContext context,
        IOptions<JwtSetting> jwtSettings, 
        IOptions<AppSettings> appSettings,
        IAppCache appCache,
        IUserValidationService userValidationService,
        ICurrentUserService currentUserService,
        IEmailSender emailSender,
        ISecurityLogger securityLogger,
        ILogger<AuthService> logger, IUserService userService,
         IConfiguration configuration, IEmailTemplateService emailTemplate)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _jwtSettings = jwtOptions.Value;
        _refreshTokenService = refreshTokenService;
        _cache = appCache;
        _userValidationService = userValidationService;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
        _securityLogger = securityLogger;
        _logger = logger;
        _userService = userService;
        _configuration = configuration;
        _emailTemplate = emailTemplate;
    }

    #region Login

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không hợp lệ"
            };
        }


        if (!user.EmailConfirmed)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Vui lòng xác nhận email trước khi đăng nhập."
            };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không hợp lệ"
            };
        }

        var validation = await _userValidationService.ValidateAsync(new()
        {
            Scenario = UserValidationScenario.Login,
            UserId = user.Id
        });

        if (!validation.Succeeded)
        {
            await _securityLogger.LogAsync(SecurityActionType.Login,false, "Tài khoản hoặc mật khẩu không hợp lệ.");
            _logger.LogWarning(validation.Error.ToString());
            
            return new AuthResponse
            {
                Success = false,
                Message = validation.Error.ToString()
            };
        }
        var roles = await _userService.GetRolesAsync(user.Id);
        var lifeTime = request.IsRemember ? TimeSpan.FromDays(_jwtSettings.RefreshTokenRememberExpirationDays) : TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

        return await GenerateAuthResponseAsync(user, roles, lifeTime);
    }

    #endregion

    #region Register

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.UserName.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email không tồn tại."
            };
        }

        if (await _userManager.FindByNameAsync(username) != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản không tồn tại."
            };
        }

        var user = new AppUser
        {
            Email = email,
            UserName = username,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            await _securityLogger.LogAsync(SecurityActionType.Register, true, 
                $"{user.UserName} đăng ký lỗi: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            return new AuthResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(x => x.Description))
            };
        }

        await _userService.AssignRoleAsync(user.Id, "User", cancellationToken);
        await _securityLogger.LogAsync(SecurityActionType.Register, true, $"{user.UserName} đăng ký thành công");
        await SendConfirmEmailAsync(user);
       
        return new AuthResponse
        {
            Success = true,
            Message = "Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản."
        };
    }

    public async Task<ServiceResult> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);

        if (user == null)
            return ServiceResult.Fail("Không tìm thấy tài khoản hợp lệ.");

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            await _securityLogger.LogAsync(SecurityActionType.ConfirmEmail, true, 
                $"{user.UserName} xác thực email lỗi: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            return ServiceResult.Fail(result.Errors.Select(x => x.Description));
        }

        await SendWelcomeEmailAsync(user);
        await _securityLogger.LogAsync(SecurityActionType.ConfirmEmail, true, $"{user.UserName} xác thực email thành công");
        
        return ServiceResult.Success();
    }
    #endregion

    #region Refresh Token

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
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
        var rotateResult = await _refreshTokenService.RotateAsync(refreshTokenEntity,  cancellationToken);
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
            ExpireAt = rotateResult.RefreshTokenEntity.ExpiredAt,
            Message = "Refresh token successful"
        };
    }
    #endregion

    #region External Login


    public async Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request,
    CancellationToken cancellationToken = default)
    {
        try
        {

            var user = await FindOrCreateExternalUserAsync(request, cancellationToken);
            if (user == null)
            {
                return AuthResponse.Fail("Không thể tạo hoặc liên kết tài khoản.");
            }
            await SaveExternalTokensAsync(user, request);
            var roles = await _userService.GetRolesAsync(user.Id);
            var lifeTime = request.RememberMe ? TimeSpan.FromDays(_jwtSettings.RefreshTokenRememberExpirationDays) : TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

            return await GenerateAuthResponseAsync(user, roles, lifeTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login failed.");
            return AuthResponse.Fail("Đăng nhập bằng tài khoản ngoài thất bại.");
        }
    }
    //public async Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request,
    //    CancellationToken cancellationToken = default)
    //{
    //    // 1. Tìm theo LoginProvider + ProviderKey
    //    var user = await _userManager.FindByLoginAsync(
    //        request.Provider,
    //        request.ProviderKey);

    //    // 2. Nếu chưa liên kết
    //    if (user == null)
    //    {
    //        if (string.IsNullOrWhiteSpace(request.Email))
    //        {
    //            // return AuthResponse.Fail("Email is required.");
    //            return AuthResponse.Fail("Không lấy được email.");
    //        }
    //        // Tìm theo email
    //        if (!string.IsNullOrWhiteSpace(request.Email))
    //        {
    //            user = await _userManager.FindByEmailAsync(request.Email);
    //        }

    //        // Chưa có user -> tạo mới
    //        if (user == null)
    //        {
    //            user = new AppUser
    //            {
    //                UserName = await GenerateUserNameAsync(request.Name ?? request.Email!),
    //                Email = request.Email,
    //                EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
    //                CreatedAt = DateTime.UtcNow,
    //                UpdatedAt = DateTime.UtcNow,
    //                Status = EntityStatus.Enabled
    //            };

    //            var createResult = await _userManager.CreateAsync(user);

    //            if (!createResult.Succeeded)
    //            {
    //                return AuthResponse.Fail(createResult.Errors.Select(x => x.Description));
    //            }
    //        }

    //        // Liên kết Google/Facebook
    //        var addLoginResult = await _userManager.AddLoginAsync(
    //            user,
    //            new UserLoginInfo(
    //                request.Provider,
    //                request.ProviderKey,
    //                request.Provider));

    //        if (!addLoginResult.Succeeded)
    //        {
    //            return AuthResponse.Fail(
    //                addLoginResult.Errors.Select(x => x.Description));
    //        }
    //    }

    //    // 3. Lưu token của Provider (nếu muốn)
    //    if (!string.IsNullOrEmpty(request.AccessToken))
    //    {
    //        await _userManager.SetAuthenticationTokenAsync(
    //            user,
    //            request.Provider,
    //            "access_token",
    //            request.AccessToken);
    //    }

    //    if (!string.IsNullOrEmpty(request.RefreshToken))
    //    {
    //        await _userManager.SetAuthenticationTokenAsync(
    //            user,
    //            request.Provider,
    //            "refresh_token",
    //            request.RefreshToken);
    //    }

    //    // 4. Sinh Access Token
    //    var accessToken = await _jwtService.GenerateAccessToken(user);

    //    // 5. Sinh Refresh Token
    //    var refreshToken = await _refreshTokenService.CreateAsync(
    //        user.Id,
    //        request.RememberMe,
    //        cancellationToken);

    //    // 6. Trả kết quả
    //    return AuthResponse.Success(
    //        accessToken,
    //        refreshToken.Token,
    //        accessToken.ExpiresAt,
    //        refreshToken.ExpiredAt);
    //}

    #endregion

    #region Logout

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshTokenEntity = await _refreshTokenService.GetByTokenAsync(refreshToken,cancellationToken);

        if (refreshTokenEntity == null)
        {
            return;
        }

        await _refreshTokenService.RevokeAllUserTokensAsync(refreshTokenEntity.UserId, cancellationToken);

        await _securityLogger.LogAsync(SecurityActionType.Logout, true, $"{refreshTokenEntity.UserId} đăng xuất thành công");
        _logger.LogInformation("Đăng xuất thành công");
        await _refreshTokenService.RevokeAsync(refreshTokenEntity, cancellationToken);
    }

    #endregion

    public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request,
    CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync($"{request.Email}");

        if (user == null)
            return ServiceResult.Fail("Tài khoản không tồn tại");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return ServiceResult.Fail("Tài khoản chưa xác minh email");

        await SendForgotPasswordEmailAsync(user);
        await _securityLogger.LogAsync(SecurityActionType.ForgotPassword, true, "Quên mật khẩu thành công đợi xác nhận.");
        return ServiceResult.Success();
    }
    public async Task<ServiceResult> ResetPasswordByTokenAsync(ResetPasswordRequest request,
    CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByIdAsync(request.UserId);

        if (user == null)
            return ServiceResult.Fail("Tài khoản không tồn tại");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

        //var decodedToken = HttpUtility.UrlDecode(request.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        
        if (!result.Succeeded)
        {
            return ServiceResult.Fail(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _userService.IncreasePermissionVersionAsync(user.Id);
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        await _securityLogger.LogAsync(SecurityActionType.ResetPassword, true, "Password reset.");
        return ServiceResult.Success(string.Join(", ", result.Errors.Select(x => x.Description)));
    }
    public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
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
        //return new AuthResponse(
        //{
        //    Success = true,
        //    Message = "Password changed successfully."
        //});
        return new AuthResponse
        {
            Success = true,
            UserId = user.Id,
            Email = user.Email!,
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

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == username);
    }
    public async Task<ServiceResult> ResendConfirmEmailAsync(string email)
{
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
        return ServiceResult.Fail("Email không tồn tại.");

    if (user.EmailConfirmed)
        return ServiceResult.Fail("Email đã được xác nhận.");

    await SendConfirmEmailAsync(user);

    return ServiceResult.Success("Đã gửi lại email xác nhận.");
}
    #endregion

    #region Private Methods

    private async Task<AuthResponse> GenerateAuthResponseAsync(AppUser user, IReadOnlyList<string> roles, TimeSpan lifeTime,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id, lifeTime, cancellationToken);
        return new AuthResponse
        {
            Success = true,
            UserId = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            AccessToken = accessToken,
            RefreshToken = refreshToken.RefreshToken,
            ExpireAt = refreshToken.ExpireAt,
            Message = "Login successful"
        };
    }

    private async Task SendConfirmEmailAsync(AppUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var frontendUrl = _configuration["BaseUrl"];
        var callbackUrl = $"{frontendUrl}/admin/auth/confirm-email/callback?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

        var html = await _emailTemplate.RenderAsync("ConfirmEmail",
            new ConfirmEmailModel
            {
                
                UserName = user.UserName!,
                ConfirmEmailUrl = callbackUrl,
                ExpireHours = 24
            });

        await _emailSender.SendEmailAsync(user.Email!, "Xác nhận tài khoản", html);
    }

    private async Task SendWelcomeEmailAsync(AppUser user)
    {
        var frontendUrl = _configuration["BaseUrl"];
        var html = await _emailTemplate.RenderAsync("Welcome",
            new SuccessEmailModel
            {
                LoginUrl = $"{frontendUrl}/admin/auth/login",
                UserName = user.UserName!,
            });

        await _emailSender.SendEmailAsync(user.Email!, "Chào mừng bạn", html);
    }
    private async Task SendForgotPasswordEmailAsync(AppUser user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var encodedToken = HttpUtility.UrlEncode(token);
        var frontendUrl = _configuration["BaseUrl"];
        var callbackUrl = $"{frontendUrl}/admin/auth/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

        var html = await _emailTemplate.RenderAsync("ResetPassword",
            new ResetPasswordEmailModel
            {
                UserName = user.UserName!,
                ResetPasswordUrl = callbackUrl,
                LogoUrl = "https://scontent.fsgn8-4.fna.fbcdn.net/v/t39.30808-6/623377577_1537695691356875_8719577165193812137_n.jpg?stp=dst-jpg_tt6&cstp=mx2048x2048&ctp=s2048x2048&_nc_cat=101&ccb=1-7&_nc_sid=6ee11a&_nc_eui2=AeFB6BR-lE0FCz1JlCdka_PU_6ErkG8qdDb_oSuQbyp0Nup9FqHqOUR09brkE_ad8Dbi57Et2dXAQIGguYzg_3e7&_nc_ohc=OPjBncn8hy8Q7kNvwGINdx4&_nc_oc=AdqKU47uWlCDz9e7KgOg11EgRAQaavSUA7-uYKEX0u7TKFy3T05UryXdT793wmA6O38&_nc_zt=23&_nc_ht=scontent.fsgn8-4.fna&_nc_gid=YO1VrraqeTiBGWu83n58-w&_nc_ss=7b2a8&oh=00_AQCEp1p5DCJzN4njmJKOXt15pTKI7etsyUETgKuoJFLYrQ&oe=6A6674F4",
                SiteName = "Caterin Việt Nam",
                SupportPhone = "0903653303",
                ExpireMinutes = 15
            });

        await _emailSender.SendEmailAsync(user.Email!, "Xác nhận tài khoản", html);
    }
    private async Task SendResetPasswordEmailAsync(AppUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var frontendUrl = _configuration["BaseUrl"];
        var callbackUrl = $"{frontendUrl}/admin/auth/forgot-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

        var html = await _emailTemplate.RenderAsync("ResetPassword",
            new ConfirmEmailModel
            {
                UserName = user.UserName!,
                ConfirmEmailUrl = callbackUrl,
                ExpireHours = 5
            });

        await _emailSender.SendEmailAsync(user.Email!, "Xác nhận tài khoản", html);
    }

    private async Task<AppUser?> FindOrCreateExternalUserAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Tìm theo Provider
        var user = await _userManager.FindByLoginAsync(request.Provider, request.ProviderKey);

        if (user != null)
            return user;
        // 2. Tìm theo Email
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user = await _userManager.FindByEmailAsync(request.Email);
        }


        bool isNewUser = false;
        // 3. Chưa có User
        if (user == null)
        {
            string? avatar = null;
            if (request.Provider == "Facebook")
            {
                avatar = $"https://graph.facebook.com/{request.ProviderKey}/picture?type=large";
            }
            else
            {
                avatar = request.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
            }
            user = new AppUser
            {
                Avatar = avatar,
                UserName = await GenerateUserNameAsync(request.Name ?? request.Email!),
                Email = request.Email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = EntityStatus.Enabled
                
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Create external user failed: {Errors}", string.Join(", ", createResult.Errors.Select(x => x.Description)));
                return null;
            }
            await _userManager.AddToRoleAsync(user, "User");
            isNewUser = true;
        }

        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    request.Provider,
                    request.ProviderKey,
                    request.Provider));

        if (!result.Succeeded)
        {
            // Có thể request khác vừa liên kết xong
            var existing = await _userManager.FindByLoginAsync(
                request.Provider,
                request.ProviderKey);

            if (existing != null)
                return existing;

            _logger.LogWarning("Create external user failed: {Errors}", string.Join(", ", result.Errors.Select(x => x.Description)));
            return null;
        }
        if (isNewUser)
        {
            await SendWelcomeEmailAsync(user);
        }
        return user;
    }
    private async Task SaveExternalTokensAsync(AppUser user, ExternalLoginRequest request)
    {
        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            await _userManager.SetAuthenticationTokenAsync(
                user,
                request.Provider,
                "access_token",
                request.AccessToken);
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _userManager.SetAuthenticationTokenAsync(
                user,
                request.Provider,
                "refresh_token",
                request.RefreshToken);
        }

        if (request.ExpiresAt.HasValue)
        {
            await _userManager.SetAuthenticationTokenAsync(
                user,
                request.Provider,
                "expires_at",
                request.ExpiresAt.Value.ToUnixTimeSeconds().ToString());
        }
    }

    private async Task<string> GenerateUserNameAsync(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            source = $"user{Random.Shared.Next(100000, 999999)}";
        }

        // Nếu là email thì lấy phần trước @
        if (source.Contains('@'))
        {
            source = source[..source.IndexOf('@')];
        }

        // Chuẩn hóa
        source = CommonHelper.NormalizeVietnamese(source)
                             .ToLowerInvariant()
                             .Replace(" ", "");

        // Chỉ giữ a-z, 0-9 và _
        source = Regex.Replace(source, @"[^a-z0-9_]", "");

        if (string.IsNullOrWhiteSpace(source))
        {
            source = "user";
        }

        var username = source;
        var index = 1;

        while (await _userManager.FindByNameAsync(username) != null)
        {
            username = $"{source}{index++}";
        }

        return username;
    }
    #endregion
}
