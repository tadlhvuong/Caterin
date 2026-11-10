using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
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
using Shared.Requests.Auth;
using Shared.Responses;
using Shared.Services.Email;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System.Data;
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

        await using var transaction =
           await _dbContext.Database.BeginTransactionAsync(
               cancellationToken);
        try
        {
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
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
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

    public async Task<ServiceResult> RegisterExternalAsync(
    ExternalRegistrationRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validate request.
        if (!request.AcceptTerms || !request.AcceptPrivacy)
        {
            return ServiceResult.Fail(
                "Bạn phải đồng ý với Điều khoản sử dụng và Chính sách bảo mật.");
        }

        if (string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.ProviderKey) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.UserName))
        {
            return ServiceResult.Fail(
                "Thông tin đăng ký không hợp lệ.");
        }

        var provider = request.Provider.Trim();

        if (!provider.Equals("Google", StringComparison.OrdinalIgnoreCase) &&
            !provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Fail(
                "Nhà cung cấp đăng nhập không được hỗ trợ.");
        }

        provider = provider.Equals("Google", StringComparison.OrdinalIgnoreCase)
            ? "Google"
            : "Facebook";

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.UserName.Trim();

        // 2. Kiểm tra provider đã được liên kết chưa.
        var existingLogin = await _userManager.FindByLoginAsync(
            provider,
            request.ProviderKey);

        if (existingLogin != null)
        {
            return ServiceResult.Fail(
                "Tài khoản Google/Facebook này đã được liên kết.");
        }

        // 3. Kiểm tra email và username.
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return ServiceResult.Fail(
                "Email đã được sử dụng. Vui lòng đăng nhập bằng tài khoản Caterin hiện có.");
        }

        if (await _userManager.FindByNameAsync(username) != null)
        {
            return ServiceResult.Fail(
                "Tên tài khoản đã tồn tại.");
        }

        var user = new AppUser
        {
            Email = email,
            UserName = username,
            DisplayName = string.IsNullOrWhiteSpace(request.Name)
                ? username
                : request.Name.Trim(),

            EmailConfirmed = false,
            IsActive = true,
            IsDeleted = false,
            Status = EntityStatus.Pending,

            CreatedAt = DateTime.UtcNow,
            CreatedIP = CommonHelper.GET_IP()
        };

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            // 4. Tạo tài khoản không có password.
            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                await transaction.RollbackAsync(cancellationToken);

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.Register,
                    false,
                    "External registration failed during user creation.");

                return ServiceResult.Fail(errors);
            }

            // 5. Gán Default Role.
            var roleResult = await _userService.AssignDefaultRoleAsync(
                user.Id,
                cancellationToken);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    "Failed to assign default role during external registration. UserId: {UserId}",
                    user.Id);

                return ServiceResult.Fail(
                    roleResult.Message ??
                    "Không thể phân quyền cho tài khoản.");
            }

            // 6. Liên kết Google/Facebook.
            var loginResult = await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo(
                    provider,
                    request.ProviderKey,
                    request.Provider));

            if (!loginResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                var errors = loginResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.Register,
                    false,
                    "External registration failed during provider linking.");

                return ServiceResult.Fail(errors);
            }

            // 7. Commit tạo user + role + external login.
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogError(
                ex,
                "External registration failed. UserId: {UserId}",
                user.Id);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.Register,
                false,
                "External registration failed unexpectedly.");

            return ServiceResult.Fail(
                "Không thể hoàn tất đăng ký. Vui lòng thử lại.");
        }

        // 8. Ghi nhận đăng ký thành công.
        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.Register,
            true,
            $"External user registered successfully. UserId: {user.Id}");

        // 9. Gửi email xác nhận sau khi transaction đã commit.
        var emailResult = await _userEmailService.SendConfirmEmailAsync(
            user,
            cancellationToken);

        if (!emailResult.Succeeded)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ResendConfirmEmail,
                false,
                $"External registration confirmation email failed. UserId: {user.Id}");

            return ServiceResult.SuccessMessage(
                "Tài khoản đã được tạo và liên kết thành công, nhưng email xác nhận chưa gửi được. Vui lòng gửi lại email xác nhận.");
        }

        return ServiceResult.SuccessMessage(
            "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.");
    }

    public async Task<ServiceResult> ConfirmEmailAsync(
    EmailAction emailAction,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailAction);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(emailAction.UserId);

        if (user == null || user.IsDeleted)
            return ServiceResult.Fail("Không tìm thấy tài khoản hợp lệ.");
        var wasEmailConfirmed = user.EmailConfirmed;
        if (user.EmailConfirmed && user.Status == EntityStatus.Active)
            return ServiceResult.Fail("Email đã được xác thực trước đó.");

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(emailAction.Token));
        }
        catch (FormatException)
        {
            return ServiceResult.Fail("Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
        }

        var isValid = await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<AppUser>.ConfirmEmailTokenPurpose,
            token);

        if (!isValid)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ConfirmEmail,
                false,
                $"Email confirmation token invalid. UserId={user.Id}");

            return ServiceResult.Fail(
                "Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
        }

        // Không tự động kích hoạt tài khoản đã bị khóa trạng thái
        // bởi quản trị viên.
        if (user.Status == EntityStatus.Pending)
        {
            user.EmailConfirmed = true;

            if (user.Status == EntityStatus.Pending && user.IsActive)
            {
                user.Status = EntityStatus.Active;
            }
        }
        else
        {
            // Với tài khoản không còn Pending, chỉ xác nhận email.
            user.EmailConfirmed = true;
        }

        await using var transaction =
    await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Cập nhật EmailConfirmed và Status.
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                return ServiceResult.Fail(
                    updateResult.Errors.Select(x => x.Description));
            }

            // Đánh dấu action đã sử dụng.
            var markResult = await _emailActionService.MarkUsedAsync(
                emailAction.Id,
                user.Id,
                EmailActionType.ConfirmEmail,
                cancellationToken);

            if (!markResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return markResult;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        if (!wasEmailConfirmed)
        {
            try
            {
                await _userEmailService.SendWelcomeEmailAsync(
                    user,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Send welcome email failed. UserId: {UserId}",
                    user.Id);
            }
        }

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.ConfirmEmail,
            true,
            $"User {user.UserName} confirmed email successfully.");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ResendConfirmEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        email = email.Trim().ToLowerInvariant();
        //Tìm user theo email user
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null ||
    user.IsDeleted ||
    !user.IsActive ||
    user.Status != EntityStatus.Pending ||
    user.EmailConfirmed)
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
    public async Task<ExternalLoginResult> ExternalLoginAsync(
     ExternalLoginRequest request,
     CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Provider) ||
                string.IsNullOrWhiteSpace(request.ProviderKey))
            {
                return ExternalLoginResult.Failed(
                    "Thông tin đăng nhập không hợp lệ.");
            }

            var provider = request.Provider.Trim();

            if (!provider.Equals("Google", StringComparison.OrdinalIgnoreCase) &&
                !provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
            {
                return ExternalLoginResult.Failed(
                    "Nhà cung cấp đăng nhập không được hỗ trợ.");
            }

            provider = provider.Equals("Google", StringComparison.OrdinalIgnoreCase)
                ? "Google"
                : "Facebook";

            var normalizedRequest = new ExternalLoginRequest
            {
                Provider = provider,
                ProviderKey = request.ProviderKey.Trim(),
                Email = request.Email?.Trim().ToLowerInvariant(),
                Name = request.Name,
                AvatarUrl = request.AvatarUrl,
                Claims = request.Claims,
                RememberMe = request.RememberMe
            };

            var resolution = await ResolveExternalLoginAsync(
                normalizedRequest,
                cancellationToken);

            switch (resolution.Status)
            {
                case ExternalLoginResolutionStatus.EmailAlreadyExists:
                    return ExternalLoginResult.EmailAlreadyExists(
                        "Email này đã được sử dụng cho tài khoản Caterin. " +
                        "Vui lòng đăng nhập bằng tài khoản hiện có, sau đó liên kết Google/Facebook trong mục Connections.");

                case ExternalLoginResolutionStatus.RegistrationRequired:
                    return ExternalLoginResult.RegistrationRequired();

                case ExternalLoginResolutionStatus.LinkedUser:
                    break;

                default:
                    return ExternalLoginResult.Failed(
                        "Không thể xử lý thông tin đăng nhập ngoài.");
            }

            var user = resolution.User;

            if (user == null)
            {
                return ExternalLoginResult.Failed(
                    "Không thể tìm thấy tài khoản liên kết.");
            }

            // Kiểm tra trạng thái tài khoản trước khi tạo session.
            var validationResult = await _userValidationService.ValidateAsync(
                new UserValidationContext
                {
                    Scenario = UserValidationScenario.Login,
                    UserId = user.Id,
                    User = user,
                    HttpContext = _httpContextAccessor.HttpContext
                },
                cancellationToken);

            if (!validationResult.Succeeded)
            {
                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ExternalLogin,
                    false,
                    $"External login rejected. UserId={user.Id}, Error={validationResult.Error}");

                return ExternalLoginResult.Failed(
                    "Tài khoản không đủ điều kiện đăng nhập.");
            }
            await UpdateExternalAvatarAsync(
    user,
    request,
    cancellationToken);

            var roles = await _userService.GetRolesAsync(user.Id);

            var lifeTime = request.RememberMe
                ? TimeSpan.FromDays(
                    _jwtSettings.RefreshTokenRememberExpirationDays)
                : TimeSpan.FromDays(
                    _jwtSettings.RefreshTokenExpirationDays);

            var authResponse = await GenerateAuthResponseAsync(
                user,
                roles,
                lifeTime,
                cancellationToken,
                SecurityActionType.ExternalLogin);

            if (!authResponse.Success)
            {
                return ExternalLoginResult.Failed(
                    authResponse.Message ??
                    "Đăng nhập bằng tài khoản ngoài thất bại.");
            }

            return ExternalLoginResult.Authenticated(authResponse);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login failed.");

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ExternalLogin,
                false,
                ex.Message);

            return ExternalLoginResult.Failed(
                "Đăng nhập bằng tài khoản ngoài thất bại.");
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
    public async Task<ServiceResult> ForgotPasswordAsync(
       ForgotPasswordRequest request,
       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        const string responseMessage =
            "Nếu email tồn tại và đủ điều kiện, chúng tôi đã gửi hướng dẫn.";

        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("Vui lòng nhập email hợp lệ.");

        var user = await _userService.FindByEmailAsync(email);

        if (user == null ||
            user.IsDeleted ||
            !user.IsActive ||
            user.Status != EntityStatus.Active ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ForgotPassword,
                false,
                "Forgot password requested for unknown or ineligible account.");

            return ServiceResult.Success(responseMessage);
        }

        var emailResult = await _userEmailService.SendForgotPasswordEmailAsync(
            user,
            cancellationToken);

        if (!emailResult.Succeeded)
        {
            _logger.LogWarning(
                "Failed to send password reset email. UserId={UserId}",
                user.Id);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ForgotPassword,
                false,
                "Failed to send password reset email.");

            return ServiceResult.Success(responseMessage);
        }

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.ForgotPassword,
            true,
            "Password reset email sent successfully.");

        return ServiceResult.Success(responseMessage);
    }
    public async Task<ServiceResult> ResetPasswordByTokenAsync(
     string key,
     ResetPasswordRequest request,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
            return ServiceResult.Fail("Liên kết đặt lại mật khẩu không hợp lệ.");

        var actionResult = await _emailActionService.GetValidAsync(
            key,
            EmailActionType.ResetPassword,
            cancellationToken);

        if (!actionResult.Succeeded || actionResult.Data == null)
            return ServiceResult.Fail(
                "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

        var emailAction = actionResult.Data;

        var user = await _userService.FindByIdAsync(emailAction.UserId);

        if (user == null ||
            user.IsDeleted ||
            !user.IsActive ||
            user.Status != EntityStatus.Active ||
            !user.EmailConfirmed)
        {
            return ServiceResult.Fail(
                "Tài khoản không tồn tại hoặc không đủ điều kiện đặt lại mật khẩu.");
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(emailAction.Token));
        }
        catch (FormatException)
        {
            return ServiceResult.Fail(
                "Token đặt lại mật khẩu không hợp lệ.");
        }

        var tokenValid = await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<AppUser>.ResetPasswordTokenPurpose,
            decodedToken);

        if (!tokenValid)
        {
            return ServiceResult.Fail(
                "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        IEnumerable<string>? resetErrors = null;

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Giành quyền sử dụng EmailAction.
            var markResult = await _emailActionService.MarkUsedAsync(
                emailAction.Id,
                user.Id,
                EmailActionType.ResetPassword,
                cancellationToken);

            if (!markResult.Succeeded)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return markResult;
            }

            // Reset password.
            var resetResult = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                request.NewPassword);

            if (!resetResult.Succeeded)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                resetErrors = resetResult.Errors
                    .Select(x => x.Description)
                    .ToArray();
            }
            else
            {
                // Thu hồi toàn bộ session và refresh token.
                await _refreshTokenService.RevokeAllUserSessionsInTransactionAsync(
                    user.Id,
                    TokenRevocationReasons.ResetPassword,
                    user.Id,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogError(
                ex,
                "Password reset failed. UserId: {UserId}",
                user.Id);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ResetPassword,
                false,
                $"Password reset failed unexpectedly. UserId={user.Id}");

            return ServiceResult.Fail(
                "Không thể đặt lại mật khẩu. Vui lòng thử lại.");
        }

        // Ghi log sau khi transaction đã kết thúc.
        if (resetErrors != null)
        {
            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ResetPassword,
                false,
                $"Password reset failed. UserId={user.Id}");

            return ServiceResult.Fail(resetErrors);
        }

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.ResetPassword,
            true,
            $"Password reset successfully. UserId={user.Id}");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ChangePasswordAsync(
     ChangePasswordRequest request,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.Fail("Bạn chưa đăng nhập.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ServiceResult.Fail(new ServiceError
            {
                Code = "PasswordRequired",
                Field = nameof(request.NewPassword),
                Message = "Vui lòng nhập mật khẩu mới."
            });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return ServiceResult.Fail(new ServiceError
            {
                Code = "PasswordMismatchConfirmation",
                Field = nameof(request.ConfirmPassword),
                Message = "Mật khẩu xác nhận không khớp."
            });
        }

        var user = await _userService.FindByIdAsync(userId);

        if (user == null ||
            user.IsDeleted ||
            !user.IsActive ||
            user.Status != EntityStatus.Active)
        {
            return ServiceResult.Fail(
                "Tài khoản không tồn tại hoặc không đủ điều kiện thiết lập mật khẩu.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);

        if (hasPassword && string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return ServiceResult.Fail(new ServiceError
            {
                Code = "CurrentPasswordRequired",
                Field = nameof(request.CurrentPassword),
                Message = "Vui lòng nhập mật khẩu hiện tại."
            });
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = hasPassword
                ? await _userManager.ChangePasswordAsync(
                    user,
                    request.CurrentPassword!,
                    request.NewPassword)
                : await _userManager.AddPasswordAsync(
                    user,
                    request.NewPassword);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                var errors = result.Errors.Select(error => new ServiceError
                {
                    Code = error.Code,
                    Field = GetPasswordErrorField(error.Code, hasPassword),
                    Message = error.Description
                }).ToList();

                await _logSafetyService.TryLogSecurityAsync(
                    SecurityActionType.ChangePassword,
                    false,
                    hasPassword
                        ? "Change password failed."
                        : "Create password failed.");

                return ServiceResult.Fail(errors);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _refreshTokenService.RevokeAllUserSessionsInTransactionAsync(
                user.Id,
                TokenRevocationReasons.PasswordChanged,
                user.Id,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogError(
                ex,
                "Password operation failed. UserId={UserId}",
                user.Id);

            await _logSafetyService.TryLogSecurityAsync(
                SecurityActionType.ChangePassword,
                false,
                "Password operation failed due to an unexpected error.");

            return ServiceResult.Fail(
                "Không thể thiết lập mật khẩu. Vui lòng thử lại.");
        }

        await _logSafetyService.TryLogSecurityAsync(
            SecurityActionType.ChangePassword,
            true,
            hasPassword
                ? "Password changed."
                : "Password created.");

        return ServiceResult.Success();
    }

    private static string? GetPasswordErrorField(
        string errorCode,
        bool hasPassword)
    {
        if (hasPassword && errorCode == "PasswordMismatch")
            return nameof(ChangePasswordRequest.CurrentPassword);

        return errorCode switch
        {
            "PasswordTooShort" or
            "PasswordRequiresNonAlphanumeric" or
            "PasswordRequiresDigit" or
            "PasswordRequiresLower" or
            "PasswordRequiresUpper" or
            "PasswordRequiresUniqueChars"
                => nameof(ChangePasswordRequest.NewPassword),

            _ => null
        };
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
     CancellationToken cancellationToken = default,
      SecurityActionType actionType = SecurityActionType.Login)
    {
        var http = _httpContextAccessor.HttpContext;
        var now = DateTime.UtcNow;
        var userAgent = http?.Request.Headers.UserAgent.ToString();
        if (http == null)
            throw new InvalidOperationException(
                "HttpContext is unavailable when creating DeviceId.");
        var deviceId = _refreshTokenService.GetOrCreateDeviceId(http);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityStamp = user.SecurityStamp!,
            DeviceId = deviceId,
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
            //Thu hồi Session cũ
            var oldSessions = await _dbContext.UserSessions
            .Where(x =>
                x.UserId == user.Id &&
                x.DeviceId == deviceId &&
                x.RevokedAt == null)
            .ToListAsync(cancellationToken);

            foreach (var oldSession in oldSessions)
            {
                oldSession.RevokedAt = now;
                oldSession.RevokedReason = "ReplacedByNewLogin";
                oldSession.RevokedById = null;
            }
            //Thu hồi RefreshToken cũ
            var oldSessionIds = oldSessions
    .Select(x => x.Id)
    .ToList();

            if (oldSessionIds.Count > 0)
            {
                await _dbContext.RefreshTokens
                    .Where(x =>
                        oldSessionIds.Contains(x.SessionId) &&
                        !x.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.IsRevoked, true)
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokedReason, "ReplacedByNewLogin"),
                        cancellationToken);
            }
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
                actionType,
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

    private async Task<ExternalLoginResolution> ResolveExternalLoginAsync(
    ExternalLoginRequest request,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.ProviderKey))
        {
            throw new ArgumentException(
                "External provider information is invalid.",
                nameof(request));
        }

        var supportedProviders = new[] { "Google", "Facebook" };

        if (!supportedProviders.Contains(
                request.Provider,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "External provider is not supported.");
        }

        // 1. Ưu tiên tìm theo Provider + ProviderKey.
        // Nếu đã liên kết thì email không còn là điều kiện đăng nhập.
        var user = await _userManager.FindByLoginAsync(
            request.Provider,
            request.ProviderKey);

        if (user != null)
        {
            return ExternalLoginResolution.Linked(user);
        }

        // 2. Provider chưa liên kết.
        // Email không được dùng để tự động liên kết tài khoản.
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim();

            user = await _userManager.FindByEmailAsync(normalizedEmail);

            if (user != null)
            {
                return ExternalLoginResolution.ExistingEmail();
            }
        }

        // 3. Chưa có tài khoản Caterin.
        // Chuyển sang quy trình hoàn tất đăng ký riêng.
        return ExternalLoginResolution.RegistrationRequired();
    }
    //private async Task SaveExternalTokensAsync(AppUser user, ExternalLoginRequest request)
    //{
    //    if (!string.IsNullOrEmpty(request.AccessToken))
    //    {
    //        await _userManager.SetAuthenticationTokenAsync(
    //            user, request.Provider, "access_token", request.AccessToken);
    //    }

    //    if (!string.IsNullOrEmpty(request.RefreshToken))
    //    {
    //        await _userManager.SetAuthenticationTokenAsync(
    //            user, request.Provider, "refresh_token", request.RefreshToken);
    //    }

    //    if (request.ExpiresAt.HasValue)
    //    {
    //        await _userManager.SetAuthenticationTokenAsync(
    //            user, request.Provider, "expires_at", request.ExpiresAt.Value.ToUnixTimeSeconds().ToString());
    //    }
    //}

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

    private static string? ResolveExternalAvatarUrl(
    ExternalLoginRequest request)
    {
        if (string.Equals(
            request.Provider,
            "Facebook",
            StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ProviderKey))
                return null;

            // Tạo URL từ ProviderKey đã được xác thực bởi OAuth.
            return $"https://graph.facebook.com/{Uri.EscapeDataString(request.ProviderKey)}/picture?type=large";
        }

        if (string.Equals(
            request.Provider,
            "Google",
            StringComparison.OrdinalIgnoreCase))
        {
            var avatarUrl = request.AvatarUrl?.Trim();

            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            if (!Uri.TryCreate(
                avatarUrl,
                UriKind.Absolute,
                out var uri))
            {
                return null;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
                return null;

            // Google avatar thường được lưu trên googleusercontent.com.
            var host = uri.Host;

            var isGoogleImage =
                host.Equals(
                    "googleusercontent.com",
                    StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(
                    ".googleusercontent.com",
                    StringComparison.OrdinalIgnoreCase);

            return isGoogleImage ? avatarUrl : null;
        }

        return null;
    }

    private async Task UpdateExternalAvatarAsync(
    AppUser user,
    ExternalLoginRequest request,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Avatar do người dùng upload lên Caterin luôn được ưu tiên.
        if (user.AvatarMediaFileId.HasValue)
            return;

        var avatarUrl = ResolveExternalAvatarUrl(request);

        if (string.IsNullOrWhiteSpace(avatarUrl))
            return;

        if (string.Equals(
            user.Avatar,
            avatarUrl,
            StringComparison.Ordinal))
        {
            return;
        }

        var oldAvatar = user.Avatar;
        user.Avatar = avatarUrl;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            // Khôi phục giá trị trong entity đang được tracking.
            user.Avatar = oldAvatar;

            _logger.LogWarning(
                "Failed to update external avatar. UserId={UserId}, Errors={Errors}",
                user.Id,
                string.Join("; ", result.Errors.Select(x => x.Code)));
        }
    }
    #endregion Helper Account
}
