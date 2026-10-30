using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Constants.Permission;
using Shared.Extensions;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;

namespace Shared.Configurations.JWT
{
    public sealed class JwtBearerEventHandler : JwtBearerEvents
    {
        private readonly ILogger<JwtBearerEventHandler> _logger;

        public JwtBearerEventHandler(IUserValidationService validationService, ILogger<JwtBearerEventHandler> logger)
        {
            _logger = logger;
        }

        public override Task MessageReceived(MessageReceivedContext context)
        {
            if (context.HttpContext.Items.TryGetValue("refreshed_access_token", out var refreshedToken)
                && refreshedToken is string token && !string.IsNullOrWhiteSpace(token))
            {
                context.Token = token;
                return Task.CompletedTask;
            }
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            var path = context.HttpContext.Request.Path;

            var isAdmin = context.HttpContext.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase);

            var userId = context.Principal?.FindFirst(ClaimConstants.UserId)?.Value;
          
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("UserId claim missing.");
                _logger.LogInformation("UserId claim missing.");
                return;
            }
            if (context.Principal?.Identity?.IsAuthenticated != true)
            {
                context.Fail("Unauthenticated");
                _logger.LogInformation("Unauthenticated");
                return;
            }
            var userValidationService = context.HttpContext.RequestServices.GetRequiredService<IUserValidationService>();

            var validationContext = new UserValidationContext
            {
                Scenario = UserValidationScenario.AccessToken,
                Principal = context.Principal,
                ValidatePermissions = isAdmin
            };
            _logger.LogWarning(
    "Before ValidateAsync: IsAdmin={IsAdmin}, ValidatePermissions={ValidatePermissions}",
    isAdmin,
    validationContext.ValidatePermissions);
            var result = await userValidationService.ValidateAsync(validationContext);
            _logger.LogWarning(
    "After ValidateAsync: ValidatePermissions={ValidatePermissions}, SnapshotNull={SnapshotNull}",
    validationContext.ValidatePermissions,
    validationContext.PermissionSnapshot == null);
            if (!result.Succeeded)
            {
                _logger.LogWarning("JWT validation failed. Error={Error}", result.Error);

                context.Fail(result.Error.ToString());
                return;
            }
            context.HttpContext.SetUserValidationContext(validationContext);
            var storedContext = context.HttpContext.GetUserValidationContext();

            _logger.LogWarning(
    "JWT Stored Context: Method={Method}, Path={Path}, SameInstance={Same}, ValidatePermissions={ValidatePermissions}, SnapshotNull={SnapshotNull}",
    context.HttpContext.Request.Method,
    context.HttpContext.Request.Path,
    ReferenceEquals(validationContext, storedContext),
    storedContext?.ValidatePermissions,
    storedContext?.PermissionSnapshot == null);
            _logger.LogInformation("Token validated");
            await base.TokenValidated(context);
        }

        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            _logger.LogWarning(context.Exception, "JWT authentication failed.");

            return Task.CompletedTask;
        }

        public override Task Challenge(JwtBearerChallengeContext context)
        {
            var request = context.Request;

            _logger.LogDebug("Unauthorized request: {Path}", request.Path);

            // Chỉ xử lý request GET tải trang HTML
            var isHtmlRequest =
                HttpMethods.IsGet(request.Method) &&
                request.Headers.Accept.Any(x =>
                    x?.Contains("text/html",
                        StringComparison.OrdinalIgnoreCase) == true);

            if (!isHtmlRequest)
                return Task.CompletedTask;

            // Ngăn JWT trả về 401 mặc định
            context.HandleResponse();

            // Xóa hai cookie dùng chung
            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");

            var path = request.Path.Value ?? string.Empty;

            var loginUrl = path.StartsWith(
                "/admin",
                StringComparison.OrdinalIgnoreCase)
                    ? "/admin/auth/login"
                    : "/account/login";

            context.Response.Redirect(loginUrl);

            return Task.CompletedTask;
        }

        public override Task Forbidden(ForbiddenContext context)
        {
            _logger.LogWarning("Forbidden request: {Path}", context.Request.Path);
            return Task.CompletedTask;
        }
    }
}
