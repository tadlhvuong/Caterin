using Shared.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants.JWT
{
    public static class JwtSettingValidator
    {
        public static void Validate(JwtSetting settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SecretKey))
                throw new InvalidOperationException("JWT SecretKey is missing");

            if (settings.SecretKey.Length < 32)
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");

            if (string.IsNullOrWhiteSpace(settings.Issuer))
                throw new InvalidOperationException("JWT Issuer is missing");

            if (string.IsNullOrWhiteSpace(settings.Audience))
                throw new InvalidOperationException("JWT Audience is missing");

            if (settings.AccessTokenExpirationMinutes <= 0)
                throw new InvalidOperationException("AccessTokenExpirationMinutes must be > 0");

            if (settings.RefreshTokenExpirationDays <= 0)
                throw new InvalidOperationException("RefreshTokenExpirationDays must be > 0");
        }
    }
}
