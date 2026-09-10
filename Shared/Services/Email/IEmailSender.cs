namespace Shared.Services.Email
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message);
        public Task SendSmsAsync(string number, string message);
    }
}
