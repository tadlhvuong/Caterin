namespace Shared.DTOs.Identity
{
    public class EmailSetting
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;

        public string SmtpHost { get; set; } = string.Empty;
    }
}
