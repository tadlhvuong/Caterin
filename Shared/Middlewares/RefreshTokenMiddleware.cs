using Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Shared.Configurations;
using Shared.Interfaces.AuthServices;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Auth;

namespace Shared.Middlewares;

public sealed class RefreshTokenMiddleware
{
    private readonly RequestDelegate _next;

    private readonly JwtSetting _jwtSettings;

    private ILogger<RefreshTokenMiddleware> _logger;

    public RefreshTokenMiddleware(
        RequestDelegate next,
        IOptions<JwtSetting> jwtOptions, ILogger<RefreshTokenMiddleware> logger)
    {
        _next = next;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    //Access token  hết hạn chạy để lấy lại token từ dựa vào refresh token
    public async Task InvokeAsync(
        HttpContext context,
        IJwtService jwtService,
        IAuthService authService)
    {
        var accessToken = context.Request.Cookies["access_token"];
        var refreshToken = context.Request.Cookies["refresh_token"];

        // Không có refresh token thì quay lại login
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await _next(context);
            return;
        }

        // Nếu access token còn hợp lệ → không cần refresh
        if (!string.IsNullOrWhiteSpace(accessToken) && !jwtService.IsTokenExpired(accessToken))
        {
            await _next(context);
            return;
        }
        //checkpoint đã refresh ở request này. reponse tự về xóa
        if (context.Items.ContainsKey("token_refreshed"))
        {
            await _next(context);
            return;
        }

        try
        {
            var result = await authService.RefreshTokenAsync(refreshToken);
            
            if (result.Success)
            {
                context.Items["token_refreshed"] = true;
                context.Response.Cookies.Append("access_token", result.AccessToken, 
                    CookieHelper.AccessToken(_jwtSettings.AccessTokenExpirationMinutes));
                context.Response.Cookies.Append( "refresh_token", result.RefreshToken, 
                    CookieHelper.RefreshToken(result.ExpireAt));
            }
            else
            {
                    context.Response.Cookies.Delete("access_token");
                    context.Response.Cookies.Delete("refresh_token");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token failed");

        }
        await _next(context);
    }
}