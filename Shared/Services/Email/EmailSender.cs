using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Shared.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var username = _configuration["Email:Username"]
                ?? throw new InvalidOperationException(
                    "Username email sender not found.");

            var password = _configuration["Email:Password"]
                ?? throw new InvalidOperationException(
                    "Password email sender not found.");

            using var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(username),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mail.To.Add(email);

            await smtp.SendMailAsync(mail, cancellationToken);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service.
            return Task.CompletedTask;
        }
    }
}
