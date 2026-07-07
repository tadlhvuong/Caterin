using Shared.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Identity
{
    public sealed class AppSettings
    {
        public string WebsiteUrl { get; set; } = string.Empty;

        public JwtSetting Jwt { get; set; } = new();

        public EmailSetting Email { get; set; } = new();
    }
}
