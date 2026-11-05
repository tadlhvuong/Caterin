using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configurations;
using Shared.Constants.Permission;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Auth;
using Shared.Enums;
using Shared.Helpers;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using System.IdentityModel.Tokens.Jwt;

namespace Shared.Middlewares;


public sealed class RefreshTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtSetting _jwtSettings;
    private readonly ILogger<RefreshTokenMiddleware> _logger;

    public RefreshTokenMiddleware(
        RequestDelegate next,
        IOptions<JwtSetting> jwtOptions,
        ILogger<RefreshTokenMiddleware> logger)
    {
        _next = next;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IJwtService jwtService,
        IAuthService authService,
        UserManager<AppUser> userManager,
        ISecurityLogger securityLogger)
    {
        var accessToken = context.Request.Cookies["access_token"];
        var refreshToken = context.Request.Cookies["refresh_token"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await _next(context);
            return;
        }

        var accessTokenExpired =
            string.IsNullOrWhiteSpace(accessToken) ||
            jwtService.IsTokenExpired(accessToken);

        var shouldRefresh = accessTokenExpired;

        // Kiểm tra PermissionVersion nếu access token còn hạn
        if (!accessTokenExpired)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken!);

                var userId = jwt.Claims
                    .FirstOrDefault(x => x.Type == ClaimConstants.UserId)?.Value;

                var versionValue = jwt.Claims
                    .FirstOrDefault(x => x.Type == ClaimConstants.PermissionVersion)?.Value;

                if (!string.IsNullOrWhiteSpace(userId) &&
                    long.TryParse(versionValue, out var jwtVersion))
                {
                    var user = await userManager.FindByIdAsync(userId);

                    shouldRefresh = user == null ||
                                    jwtVersion != user.PermissionVersion;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to inspect access token. TraceId={TraceId}",
                    context.TraceIdentifier);
            }
        }

        if (!shouldRefresh)
        {
            await _next(context);
            return;
        }

        if (context.Items.ContainsKey("token_refreshed"))
        {
            await _next(context);
            return;
        }

        try
        {
            var result = await authService.RefreshTokenAsync(
                refreshToken,
                context.RequestAborted);

            if (result.Success)
            {
                context.Items["token_refreshed"] = true;

                context.Response.Cookies.Append(
                    "access_token",
                    result.AccessToken,
                    CookieHelper.AccessToken(
                        _jwtSettings.AccessTokenExpirationMinutes));

                context.Response.Cookies.Append(
                    "refresh_token",
                    result.RefreshToken,
                    CookieHelper.RefreshToken(result.ExpireAt));

                context.Items["refreshed_access_token"] = result.AccessToken;

                // Redirect GET HTML để request tiếp theo sử dụng cookie mới
                if (HttpMethods.IsGet(context.Request.Method) &&
                    context.Request.Headers.Accept.Any(x =>
                        x?.Contains(
                            "text/html",
                            StringComparison.OrdinalIgnoreCase) == true))
                {
                    context.Response.Redirect(
                        context.Request.PathBase +
                        context.Request.Path +
                        context.Request.QueryString);

                    return;
                }
            }
            else
            {
                // ConcurrentConflict: giữ cookie để request khác
                // có cơ hội hoàn tất rotation.
                if (result.ErrorCode == RotateTokenError.ConcurrentConflict)
                {
                    context.Response.StatusCode =
                        StatusCodes.Status409Conflict;

                    return;
                }

                // Các lỗi xác thực/refresh token thực sự
                context.Response.Cookies.Delete(
                    "access_token",
                    CookieHelper.Delete());

                context.Response.Cookies.Delete(
                    "refresh_token",
                    CookieHelper.Delete());

                if (HttpMethods.IsGet(context.Request.Method) &&
                    context.Request.Headers.Accept.Any(x =>
                        x?.Contains(
                            "text/html",
                            StringComparison.OrdinalIgnoreCase) == true))
                {
                    var path = context.Request.Path.Value ?? string.Empty;

                    var loginUrl = path.StartsWith(
                        "/admin",
                        StringComparison.OrdinalIgnoreCase)
                            ? "/admin/auth/login"
                            : "/account/login";

                    context.Response.Redirect(loginUrl);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Refresh token middleware failed. TraceId={TraceId}, Path={Path}",
                context.TraceIdentifier,
                context.Request.Path);

            try
            {
                await securityLogger.LogAsync(
                    SecurityActionType.RefreshToken,
                    false,
                    $"Refresh token processing exception. " +
                    $"ExceptionType={ex.GetType().Name}, " +
                    $"TraceId={context.TraceIdentifier}");
            }
            catch (Exception logEx)
            {
                _logger.LogError(
                    logEx,
                    "Failed to write refresh token security log.");
            }

            // Không xóa cookie khi gặp lỗi hệ thống.
            // Cho phép client nhận lỗi xác thực hoặc thử lại.
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode =
                    StatusCodes.Status503ServiceUnavailable;

                return;
            }

            throw;
        }

        await _next(context);
    }
}
