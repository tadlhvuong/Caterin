using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants.Core;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Services.Authentication;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    private readonly IJwtService _jwtService;
    private readonly JwtSetting _jwtSettings;

    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserValidationService _userValidationService;

    private readonly IRefreshTokenService _refreshTokenService;

    private readonly IEmailActionService _emailActionService;
    private readonly IUserEmailService _userEmailService;

    private readonly ILogger<AuthService> _logger;
    private readonly ILogSafetyService _logSafetyService;

    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        IJwtService jwtService, IRefreshTokenService refreshTokenService,
        AppDbContext dbContext, IAppCache appCache, IConfiguration configuration,
        IOptions<JwtSetting> jwtSettings, IOptions<AppSettings> appSettings,
        IUserService userService, IUserValidationService userValidationService, ICurrentUserService currentUserService,
        IEmailSender emailSender, IEmailActionService emailActionService,IEmailTemplateService emailTemplate, IUserEmailService userEmailService,
        ILogSafetyService logSafetyService, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;

        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;

        _refreshTokenService = refreshTokenService;

        _userService = userService;
        _userValidationService = userValidationService;
        _currentUserService = currentUserService;

        _emailActionService = emailActionService;
        _userEmailService = userEmailService;

        _logSafetyService = logSafetyService;
        _logger = logger;

        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Login

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        //Find user
        var user = await _userService.FindByEmailAsync(request.Email);
        if (user == null)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.Login, false, $"Login failed: email not found");
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không hợp lệ"
            };
        }

        //Check user email confirmed
        if (!user.EmailConfirmed)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.Login, false, "Login failed: email not confirmed");
            return new AuthResponse
            {
                Success = false,
                Message = "Vui lòng xác nhận email trước khi đăng nhập."
            };
        }
        var validation = await _userValidationService.ValidateAsync(new()
        {
            Scenario = UserValidationScenario.Login,
            UserId = user.Id
        }, cancellationToken);

        if (!validation.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Login,
                false,
                $"Login failed: {validation.Error}");

            _logger.LogWarning(
                "Login failed for user {UserId}: {Reason}",
                user.Id,
                validation.Error);

            return new AuthResponse
            {
                Success = false,
                Message = validation.Error switch
                {
                    UserValidationError.EmailNotConfirmed =>
                        "Vui lòng xác nhận email trước khi đăng nhập.",

                    UserValidationError.UserLockedOut =>
                        "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.",

                    UserValidationError.UserSuspended =>
                        "Tài khoản đã bị đình chỉ.",

                    UserValidationError.UserDeactivated =>
                        "Tài khoản đã ngừng hoạt động.",

                    _ => "Tài khoản hiện không thể đăng nhập."
                }
            };
        }

        //Sign in
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Login,
                false,
                "Login failed: account temporarily locked");

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

            var message = "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.";

            if (lockoutEnd.HasValue)
            {
                var remaining = lockoutEnd.Value - DateTimeOffset.UtcNow;

                if (remaining > TimeSpan.Zero)
                {
                    var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
                    message = $"Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {minutes} phút.";
                }
            }

            return new AuthResponse
            {
                Success = false,
                Message = message
            };
        }
        //Check sign in
        if (!signInResult.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.Login, false, "Login failed: invalid password");

            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản hoặc mật khẩu không hợp lệ"
            };
        }

        user.LastLogin = DateTime.UtcNow;
        user.LastLoginIP = CommonHelper.GET_IP();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            _logger.LogError(
                "Failed to update LastLoginAt for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", updateResult.Errors.Select(x => x.Description)));
        }
        //Lấy vai trò của user
        var roles = await _userService.GetRolesAsync(user.Id);
        var lifeTime = request.IsRemember ? TimeSpan.FromDays(_jwtSettings.RefreshTokenRememberExpirationDays) : TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
        //Tạo Auth cho user: Access token + refresh token
        return await GenerateAuthResponseAsync(user, roles, lifeTime, cancellationToken);
    }

    #endregion

    #region Register
    public async Task<AuthResponse> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.UserName.Trim();

        if (!request.AcceptTerms)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Bạn phải đồng ý với Điều khoản sử dụng."
            };
        }

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Register,
                false,
                "Register failed: email already exists.");

            return new AuthResponse
            {
                Success = false,
                Message = "Đăng ký lỗi: Email đã tồn tại."
            };
        }

        if (await _userManager.FindByNameAsync(username) != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản đã tồn tại."
            };
        }

        var user = new AppUser
        {
            Email = email,
            UserName = username,
            DisplayName = username,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false,
            IsActive = true,
            IsDeleted = false,
            Status = EntityStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedIP = CommonHelper.GET_IP()
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Register,
                false,
                $"Registration failed. UserId: {user.Id}. Errors: {errors}");

            return new AuthResponse
            {
                Success = false,
                Message = errors
            };
        }

        // Gán role mặc định.
        try
        {
            var roleResult = await _userService.AssignDefaultRoleAsync(
                user.Id,
                cancellationToken);

            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to assign default role during registration. UserId: {UserId}. Error: {Error}",
                    user.Id,
                    roleResult.Message);

                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản đã được tạo nhưng chưa thể hoàn tất phân quyền. Vui lòng liên hệ quản trị viên."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error assigning default role during registration. UserId: {UserId}",
                user.Id);

            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản đã được tạo nhưng chưa thể hoàn tất phân quyền. Vui lòng liên hệ quản trị viên."
            };
        }

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.Register,
            true,
            $"User registered successfully. UserId: {user.Id}");

        // Gửi email xác thực.
        var emailResult = await _userEmailService.SendConfirmEmailAsync(
            user,
            cancellationToken);

        if (!emailResult.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ResendConfirmEmail,
                false,
                $"Confirmation email sending failed. UserId: {user.Id}");

            return new AuthResponse
            {
                Success = true,
                Message = "Tài khoản đã được tạo nhưng email xác thực chưa gửi được. Vui lòng thử gửi lại email xác thực."
            };
        }

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản."
        };
    }
    public async Task<ServiceResult> ConfirmEmailAsync(EmailAction emailAction, CancellationToken cancellationToken = default)
    {
        //Tìm user theo id
        var user = await _userManager.FindByIdAsync(emailAction.UserId);

        if (user == null)
            return ServiceResult.Fail("Không tìm thấy tài khoản hợp lệ.");

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(emailAction.Token));
        //Xác thực email
        var result = await _userManager.ConfirmEmailAsync(user, token);
        //Check xác thực email đã thành công chưa
        if (!result.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ConfirmEmail, false,
                $"{user.UserName} xác thực email lỗi: {string.Join(", ", result.Errors.Select(x => x.Description))}");

            return ServiceResult.Fail(result.Errors.Select(x => x.Description));
        }


        if (user.Status == EntityStatus.Pending)
        {
            user.Status = EntityStatus.Active;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to activate user after email confirmation. UserId: {UserId}. Errors: {Errors}",
                    user.Id,
                    string.Join(", ", updateResult.Errors.Select(x => x.Description)));

                return ServiceResult.Fail(
                    "Email đã được xác thực nhưng không thể cập nhật trạng thái tài khoản.");
            }
        }
        //Check mail xác thực còn tồn tại không
        var markResult = await _emailActionService.MarkUsedAsync(emailAction.Id, cancellationToken);

        if (!markResult.Succeeded)
        {
            _logger.LogWarning("Cannot mark EmailAction {EmailActionId} as used.", emailAction.Id);
        }
        try
        {
            //Gửi mail báo xác thực thành công
            await _userEmailService.SendWelcomeEmailAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send welcome email failed. UserId: {UserId}", user.Id);
        }
        await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ConfirmEmail, true, $"{user.UserName} xác thực email thành công");

        return ServiceResult.Success();
    }
    
    public async Task<ServiceResult> ResendConfirmEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        email = email.Trim().ToLowerInvariant();
        //Tìm user theo email user
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || user.EmailConfirmed)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResendConfirmEmail, true, "Resend confirm email ignored.");

            return ServiceResult.Success("Nếu email tồn tại trong hệ thống và chưa được xác thực, chúng tôi đã gửi email xác thực.");
        }
        //Gửi lại email xác thực
        var result = await _userEmailService.SendConfirmEmailAsync(user, cancellationToken);

        if (!result.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResendConfirmEmail, false, $"Resend confirm email failed. UserId: {user.Id}");

            return result;
        }

        await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResendConfirmEmail, true, $"Resend confirm email success. UserId: {user.Id}");

        return ServiceResult.Success("Nếu email tồn tại trong hệ thống và chưa được xác thực, chúng tôi đã gửi email xác thực.");
    }
    
    #endregion

    #region Refresh Token

    public async Task<AuthResponse> RefreshTokenAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
    {
        // 1. Load Refresh Token
        var refreshTokenEntity = await _refreshTokenService.GetByTokenAsync(
            refreshToken,
            cancellationToken);

        if (refreshTokenEntity?.User is null)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.RefreshToken,
                false,
                "Refresh token invalid");

            return new AuthResponse
            {
                Success = false,
                ErrorCode = RotateTokenError.TokenNotFound,
                Message = "Refresh token invalid"
            };
        }

        // 2. Xác định token đã rotation
        var isRotationRetry =
            refreshTokenEntity.IsRevoked &&
            refreshTokenEntity.RevokedReason == "RefreshTokenRotated";

        // 3. Validate User, Session và SecurityStamp
        var validation = await _userValidationService.ValidateAsync(
            new UserValidationContext
            {
                Scenario = UserValidationScenario.RefreshToken,
                UserId = refreshTokenEntity.UserId,
                RefreshTokenEntity = refreshTokenEntity,
                IsRotationRetry = isRotationRetry
            },
            cancellationToken);

        if (!validation.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.RefreshToken,
                false,
                $"Refresh token validation failed. Error={validation.Error}");

            return new AuthResponse
            {
                Success = false,
                ErrorCode = MapValidationError(validation.Error),
                Message = validation.Error.ToString()
            };
        }

        // 4. Rotate Refresh Token
        var rotateResult = await _refreshTokenService.RotateAsync(
            refreshTokenEntity,
            cancellationToken);

        if (!rotateResult.Success)
        {
            // Reuse đã được ghi SecurityLog bên trong RotateAsync
            if (rotateResult.ErrorCode != RotateTokenError.ReuseDetected)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.RefreshToken,
                    false,
                    $"Refresh token rotation failed. Error={rotateResult.ErrorCode}");
            }

            return new AuthResponse
            {
                Success = false,
                ErrorCode = rotateResult.ErrorCode,
                Message = rotateResult.Error ?? "Refresh token đổi không hợp lệ"
            };
        }

        // 5. Lấy Roles
        var roles = await _userManager.GetRolesAsync(rotateResult.User);

        // 6. Tạo Access Token mới
        var accessToken = _jwtService.GenerateAccessToken(
            rotateResult.User,
            roles,
            rotateResult.RefreshTokenEntity.SessionId);

        return new AuthResponse
        {
            Success = true,
            ErrorCode = RotateTokenError.None,
            UserId = rotateResult.User.Id,
            Email = rotateResult.User.Email!,
            UserName = rotateResult.User.UserName!,
            AccessToken = accessToken,
            RefreshToken = rotateResult.RefreshToken,
            ExpireAt = rotateResult.RefreshTokenEntity.ExpiredAt,
            Message = "Refresh token đổi thành công"
        };
    }

    #endregion

    #region External Login
    public async Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request,
    CancellationToken cancellationToken = default)
    {
        try
        {
            //Tìm/Tạo user liên kết
            var user = await FindOrCreateExternalUserAsync(request, cancellationToken);

            if (user == null)
            {
                return AuthResponse.Fail("Không thể tạo hoặc liên kết tài khoản.");
            }
            await SaveExternalTokensAsync(user, request);
            
            var roles = await _userService.GetRolesAsync(user.Id);
            var lifeTime = request.RememberMe ? TimeSpan.FromDays(_jwtSettings.RefreshTokenRememberExpirationDays) : TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
            
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ExternalLogin, true, $"{user.UserName} external login success");
            return await GenerateAuthResponseAsync(user, roles, lifeTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login failed.");
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ExternalLogin, false, ex.Message);
            return AuthResponse.Fail("Đăng nhập bằng tài khoản ngoài thất bại.");
        }
    }
    #endregion

    #region LOGOUT

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var refreshTokenEntity = await _refreshTokenService.GetByTokenAsync(
            refreshToken,
            cancellationToken);

        if (refreshTokenEntity == null)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Logout,
                false,
                "Logout failed: invalid refresh token");

            return;
        }

        await _refreshTokenService.RevokeSessionAsync(
            refreshTokenEntity.UserId,
            refreshTokenEntity.SessionId,
            "UserLogout",
            cancellationToken: cancellationToken);

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.Logout,
            true,
            $"User logged out. UserId={refreshTokenEntity.UserId}, SessionId={refreshTokenEntity.SessionId}");

        _logger.LogInformation(
            "User logged out. UserId={UserId}, SessionId={SessionId}",
            refreshTokenEntity.UserId,
            refreshTokenEntity.SessionId);
    }

    public async Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        await _refreshTokenService.RevokeAllUserSessionsAsync(
            userId,
            "UserLogoutAll",
            cancellationToken: cancellationToken);

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.Logout,
            true,
            $"User logged out from all sessions. UserId={userId}");

        _logger.LogInformation(
            "User logged out from all sessions. UserId={UserId}",
            userId);
    }

    #endregion LOGOUT

    #region Change Password
    public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync($"{request.Email}");

        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ForgotPassword, false, "Forgot password requested for unknown email");
            return ServiceResult.Fail("Nếu email tồn tại và đủ điều kiện, chúng tôi đã gửi hướng dẫn.");
        }

        //Gửi email quên mật khẩu
        var emailResult = await _userEmailService.SendForgotPasswordEmailAsync(user, cancellationToken);
        if (!emailResult.Succeeded)
        {
            try
            {
                await _logSafetyService.TryLogSecurityAsync(
                 SecurityActionType.ForgotPassword,
                 false,
                 "Failed to send password reset email.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to log forgot password security event. UserId: {UserId}",
                    user.Id);
            }
            return ServiceResult.Fail("Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
        }
        try
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ForgotPassword,
                true,
                "Password reset email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to log forgot password security event. UserId: {UserId}",
                user.Id);
        };
        return ServiceResult.Success();
    }
    
    public async Task<ServiceResult> ResetPasswordByTokenAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var actionResult = await _emailActionService.GetValidAsync(request.Key, cancellationToken);

        if (!actionResult.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResetPassword, false, "Invalid reset password token");
            return ServiceResult.Fail(actionResult.Errors);
        }

        var emailAction = actionResult.Data!;

        var user = await _userService.FindByIdAsync(emailAction.UserId);

        if (user == null)
            return ServiceResult.Fail("Tài khoản không tồn tại.");
        
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(emailAction.Token));
        //Reset password
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResetPassword, false, "Reset password failed");
            return ServiceResult.Fail(result.Errors.Select(x => x.Description));
        }
        //Kiểm tra hành động reset pass còn tồn tại không
        var markResult = await _emailActionService.MarkUsedAsync(emailAction.Id, cancellationToken);
        if (!markResult.Succeeded)
        {
            _logger.LogWarning("Cannot mark EmailAction ResetPassword {EmailActionId} as used.", emailAction.Id);
        }
        //Vô hiệu hóa các phiên đăng nhập/cookie Identity cũ
        await _userManager.UpdateSecurityStampAsync(user);
        //Vô hiệu hóa quyền hạn cũ của user
        await _userService.IncreasePermissionVersionAsync(user.Id, cancellationToken);
        //Revoke toàn bộ token của user hiện tại
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id, TokenRevocationReasons.ResetPassword, cancellationToken);

        await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ResetPassword, true, "Password reset.");

        return ServiceResult.Success();
    }
    
    public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.Fail("Bạn chưa đăng nhập.");

        var user = await _userService.FindByIdAsync(userId);

        if (user == null)
            return ServiceResult.Fail("Không tìm thấy tài khoản.");
        //Đổi mật khẩu
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ChangePassword, false, "Change password failed");
            return ServiceResult.Fail(result.Errors.Select(x => x.Description));
        }

        //Vô hiệu hóa các phiên đăng nhập/cookie Identity cũ
        await _userManager.UpdateSecurityStampAsync(user);
        //Vô hiệu hóa quyền hạn cũ của user
        await _userService.IncreasePermissionVersionAsync(user.Id, cancellationToken);
        //Revoke toàn bộ token của user hiện tại
        await _refreshTokenService.RevokeAllUserTokensAsync(user.Id, TokenRevocationReasons.PasswordChanged ,cancellationToken);

        await _logSafetyService.TryLogSecurityAsync(SecurityActionType.ChangePassword, true, "Password changed.");
        return ServiceResult.Success();
    }
    
    #endregion Change Password

    #region Validate Exists
    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _userManager.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == username);
    }
    #endregion

    #region Helper Account
    private async Task<AuthResponse> GenerateAuthResponseAsync(
     AppUser user,
     IReadOnlyList<string> roles,
     TimeSpan lifeTime,
     CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var now = DateTime.UtcNow;
        var userAgent = http?.Request.Headers.UserAgent.ToString();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceName = CommonHelper.DeviceNameHelper.GetDeviceName(userAgent),
            UserAgent = userAgent,
            IpAddress = CommonHelper.GetClientIp(http),
            CreatedAt = now,
            LastActivityAt = now,
            ExpiresAt = now.Add(lifeTime)
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Tạo Session
            _dbContext.UserSessions.Add(session);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Tạo Access Token
            var accessToken = _jwtService.GenerateAccessToken(
                user, roles, session.Id);

            // 3. Tạo Refresh Token
            var refreshToken = await _refreshTokenService.CreateAsync(
                user.Id,
                session.Id,
                lifeTime,
                cancellationToken);

            // 4. Commit
            await transaction.CommitAsync(cancellationToken);

            // 5. Ghi Security Log (không ảnh hưởng kết quả login)
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Login,
                true,
                $"{user.UserName} login success");

            return new AuthResponse
            {
                Success = true,
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                AccessToken = accessToken,
                RefreshToken = refreshToken.RefreshToken,
                ExpireAt = refreshToken.ExpireAt,
                Message = "Đăng nhập thành công"
            };
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Failed to rollback authentication transaction. UserId={UserId}",
                    user.Id);
            }

            _logger.LogError(
                ex,
                "Failed to create authentication session. UserId={UserId}",
                user.Id);

            throw;
        }
    }

   
    private async Task<AppUser?> FindOrCreateExternalUserAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default)
    {
        // Tìm theo Provider
        var user = await _userManager.FindByLoginAsync(request.Provider, request.ProviderKey);

        if (user != null)
            return user;
        //Tìm theo Email
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user = await _userManager.FindByEmailAsync(request.Email);
        }


        bool isNewUser = false;
        //Chưa có User
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
                Status = EntityStatus.Active

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

        //Đã có user
        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    request.Provider, request.ProviderKey, request.Provider));

        if (!result.Succeeded)
        {
            // Có thể request khác vừa liên kết xong
            var existing = await _userManager.FindByLoginAsync(request.Provider, request.ProviderKey);

            if (existing != null)
                return existing;

            _logger.LogWarning("Create external user failed: {Errors}", string.Join(", ", result.Errors.Select(x => x.Description)));
            return null;
        }
        if (isNewUser)
        {
            await _userEmailService.SendWelcomeEmailAsync(user);
        }
        return user;
    }
    private async Task SaveExternalTokensAsync(AppUser user, ExternalLoginRequest request)
    {
        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            await _userManager.SetAuthenticationTokenAsync(
                user, request.Provider, "access_token", request.AccessToken);
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _userManager.SetAuthenticationTokenAsync(
                user, request.Provider, "refresh_token", request.RefreshToken);
        }

        if (request.ExpiresAt.HasValue)
        {
            await _userManager.SetAuthenticationTokenAsync(
                user, request.Provider, "expires_at", request.ExpiresAt.Value.ToUnixTimeSeconds().ToString());
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
        source = CommonHelper.NormalizeVietnamese(source).ToLowerInvariant() .Replace(" ", "");

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

    private static RotateTokenError MapValidationError(UserValidationError error)
    {
        return error switch
        {
            UserValidationError.UserNotFound => RotateTokenError.TokenNotFound,

            UserValidationError.RefreshTokenNotFound => RotateTokenError.TokenNotFound,
            UserValidationError.InvalidRefreshToken => RotateTokenError.TokenNotFound,
            UserValidationError.RefreshTokenRevoked => RotateTokenError.TokenRevoked,
            UserValidationError.RefreshTokenExpired => RotateTokenError.TokenExpired,

            UserValidationError.SessionInvalid => RotateTokenError.SessionInvalid,

            // Các lỗi tài khoản hoặc Security Stamp
            // không thuộc nhóm lỗi rotation cụ thể.
            _ => RotateTokenError.TokenRevoked
        };
    }
    #endregion Helper Account
}
