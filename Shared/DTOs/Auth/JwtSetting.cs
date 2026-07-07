using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Auth
{
    public class JwtSetting
    {
        public string SecretKey { get; set; } = default!;

        public string Issuer { get; set; } = default!;

        public string Audience { get; set; } = default!;

        public int AccessTokenExpirationMinutes { get; set; }

        public int RefreshTokenExpirationDays { get; set; }
    }
}
