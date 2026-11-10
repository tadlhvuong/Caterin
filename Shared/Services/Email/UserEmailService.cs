using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Notification;
using Shared.Enums;
using Shared.Interfaces.Log;
using Shared.Responses;
using Shared.Services.Authentication;
using Shared.Services.Email.EmailModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email
{
    public class UserEmailService : IUserEmailService
    {
        private readonly UserManager<AppUser> _userManager;

        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplate;
        private readonly IEmailActionService _emailActionService;

        private readonly ILogger<UserEmailService> _logger;
        private readonly ILogSafetyService _logSafetyService;
        public UserEmailService(UserManager<AppUser> userManager, IConfiguration configuration, IEmailSender emailSender, IEmailTemplateService emailTemplateService,
            IEmailActionService emailActionService, ILogger<UserEmailService> logger, ILogSafetyService logSafetyService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _emailTemplate = emailTemplateService;
            _emailActionService = emailActionService;

            _logger = logger;
            _logSafetyService = logSafetyService;
        }
        public async Task<ServiceResult> SendConfirmEmailAsync(
     AppUser user,
     CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var hours = _configuration.GetValue<int?>(
                    "ExpireHoursConfirmEmail") ?? 10;

                if (hours <= 0)
                {
                    _logger.LogError(
                        "Invalid email confirmation expiration hours.");

                    return ServiceResult.Fail(
                        "Không thể gửi email xác thực. Vui lòng thử lại sau.");
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return ServiceResult.Fail(
                        "Tài khoản chưa có địa chỉ email hợp lệ.");
                }

                var baseUrl = _configuration["BaseUrl"]?.TrimEnd('/');

                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
                    || (baseUri.Scheme != Uri.UriSchemeHttps
                        && !baseUri.IsLoopback))
                {
                    _logger.LogError("Invalid BaseUrl configuration.");

                    return ServiceResult.Fail(
                        "Không thể gửi email xác thực. Vui lòng thử lại sau.");
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                token = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                var key = await _emailActionService.CreateReplacingAsync(
                    user.Id,
                    EmailActionType.ConfirmEmail,
                    token,
                    TimeSpan.FromHours(hours),
                    reason: "Resend confirmation email",
                    cancellationToken);

                var callbackUrl = $"{baseUrl}/admin/auth/e/{key}";

                var html = await _emailTemplate.RenderAsync(
                    "ConfirmEmail",
                    new ConfirmEmailModel
                    {
                        UserName = user.UserName!,
                        ConfirmEmailUrl = callbackUrl,
                        ExpireHours = hours
                    },  cancellationToken);

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Xác nhận tài khoản",
                    html, cancellationToken);

                return ServiceResult.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Send confirmation email failed. UserId: {UserId}",
                    user.Id);

                return ServiceResult.Fail(
                    "Không thể gửi email xác thực. Vui lòng thử lại sau.");
            }
        }

        public async Task SendWelcomeEmailAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            var baseUrl = _configuration["BaseUrl"];
            var html = await _emailTemplate.RenderAsync("Welcome",
                new SuccessEmailModel
                {
                    LoginUrl = $"{baseUrl}/admin/auth/login",
                    UserName = user.UserName!,
                }, cancellationToken);

            await _emailSender.SendEmailAsync(user.Email!, "Chào mừng bạn", html, cancellationToken);
        }

        public async Task<ServiceResult> SendForgotPasswordEmailAsync(
    AppUser user,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                    return ServiceResult.Fail("Email tài khoản không hợp lệ.");

                var minutes = _configuration.GetValue<int?>(
                    "ExpireMinutesChangePass") ?? 10;

                if (minutes <= 0)
                    throw new InvalidOperationException(
                        "ExpireMinutesChangePass must be greater than zero.");

                var baseUrl = _configuration["BaseUrl"]?.TrimEnd('/');

                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
                    || (baseUri.Scheme != Uri.UriSchemeHttps
                        && !baseUri.IsLoopback))
                {
                    throw new InvalidOperationException(
                        "BaseUrl is invalid or must use HTTPS.");
                }

                // Tạo Identity Password Reset Token.
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                token = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                // Thu hồi link cũ và tạo link mới trong cùng transaction.
                var key = await _emailActionService.CreateReplacingAsync(
                    user.Id,
                    EmailActionType.ResetPassword,
                    token,
                    TimeSpan.FromMinutes(minutes),
                    reason: "Resend password reset email",
                    cancellationToken);

                var callbackUrl = $"{baseUrl}/admin/auth/e/{key}";

                var html = await _emailTemplate.RenderAsync(
                    "ResetPassword",
                    new ResetPasswordEmailModel
                    {
                        UserName = user.UserName!,
                        ResetPasswordUrl = callbackUrl,
                        LogoUrl = _configuration["Email:LogoUrl"],
                        SiteName = _configuration["Email:SiteName"]
                            ?? "Caterin Việt Nam",
                        SupportPhone = _configuration["Email:SupportPhone"],
                        ExpireMinutes = minutes
                    },
                    cancellationToken);

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Đặt lại mật khẩu",
                    html,
                    cancellationToken);

                return ServiceResult.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Send password reset email failed. UserId={UserId}",
                    user.Id);

                return ServiceResult.Fail(
                    "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
            }
        }

    }
}
