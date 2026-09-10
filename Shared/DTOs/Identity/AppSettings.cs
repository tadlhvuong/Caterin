using Shared.DTOs.Auth;

namespace Shared.DTOs.Identity
{
    public sealed class AppSettings
    {
        public string WebsiteUrl { get; set; } = string.Empty;

        public JwtSetting Jwt { get; set; } = new();

        public EmailSetting Email { get; set; } = new();
    }
}
