using Microsoft.AspNetCore.Http;

namespace Shared.Helpers;

public static class CookieHelper
{
    public static CookieOptions AccessToken(int exprired)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(exprired)
        };
    }

    public static CookieOptions RefreshToken(int exprired)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(exprired)
        };
    }
}