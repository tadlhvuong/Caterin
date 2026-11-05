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
        public async Task<ServiceResult> SendConfirmEmailAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            try
            {
                var hours = _configuration.GetValue<int?>("ExpireHoursConfirmEmail") ?? 10;
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                await _emailActionService.RevokeAsync(user.Id, EmailActionType.ConfirmEmail, reason: "Resend confirm email", cancellationToken);

                var baseUrl = _configuration["BaseUrl"];
                var key = await _emailActionService.CreateAsync(
                    user.Id,
                    EmailActionType.ConfirmEmail,
                    token,
                    TimeSpan.FromHours(hours),
                    cancellationToken);
                var callbackUrl = $"{baseUrl}/admin/auth/e/{key}";

                var html = await _emailTemplate.RenderAsync("ConfirmEmail",
                    new ConfirmEmailModel
                    {
                        UserName = user.UserName!,
                        ConfirmEmailUrl = callbackUrl,
                        ExpireHours = hours
                    });

                await _emailSender.SendEmailAsync(user.Email!, "Xác nhận tài khoản", html);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send confirm email failed. UserId: {UserId}", user.Id);

                return ServiceResult.Fail("Không thể gửi email xác thực. Vui lòng thử lại sau.");
            }
        }

        public async Task SendWelcomeEmailAsync(AppUser user)
        {
            var baseUrl = _configuration["BaseUrl"];
            var html = await _emailTemplate.RenderAsync("Welcome",
                new SuccessEmailModel
                {
                    LoginUrl = $"{baseUrl}/admin/auth/login",
                    UserName = user.UserName!,
                });

            await _emailSender.SendEmailAsync(user.Email!, "Chào mừng bạn", html);
        }
        public async Task<ServiceResult> SendForgotPasswordEmailAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            try
            {
                var minutes = _configuration.GetValue<int?>("ExpireMinutesChangePass") ?? 10;

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                await _emailActionService.RevokeAsync(
                    user.Id,
                    EmailActionType.ResetPassword,
                    reason: "Resend password reset email",
                    cancellationToken);

                var key = await _emailActionService.CreateAsync(
                    user.Id,
                    EmailActionType.ResetPassword,
                    token,
                    TimeSpan.FromMinutes(minutes),
                    cancellationToken);

                var baseUrl = _configuration["BaseUrl"];
                var callbackUrl = $"{baseUrl}/admin/auth/e/{key}";

                var html = await _emailTemplate.RenderAsync("ResetPassword",
                    new ResetPasswordEmailModel
                    {
                        UserName = user.UserName!,
                        ResetPasswordUrl = callbackUrl,
                        LogoUrl = _configuration["Email:LogoUrl"],
                        SiteName = _configuration["Email:SiteName"] ?? "Caterin Việt Nam",
                        SupportPhone = _configuration["Email:SupportPhone"],
                        ExpireMinutes = minutes
                    });

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Đặt lại mật khẩu",
                    html);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Send password reset email failed. UserId: {UserId}",
                    user.Id);

                return ServiceResult.Fail(
                    "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
            }
        }
    }
}
