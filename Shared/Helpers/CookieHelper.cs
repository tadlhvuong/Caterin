using Microsoft.AspNetCore.Http;

namespace Shared.Helpers
{
    public static class CookieHelper
{
    public static CookieOptions AccessToken(int expired)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(expired)
        };
    }

    public static CookieOptions RefreshToken(DateTime expiresAt)
    {
        var utcExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = new DateTimeOffset(utcExpiresAt)
        };
    }

    public static CookieOptions Delete()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = TimeSpan.Zero
        };
    }
}
}