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
            var isAdmin = context.Request.Path.StartsWithSegments(
                "/admin",
                StringComparison.OrdinalIgnoreCase);

            var userId = context.Principal?
                .FindFirst(ClaimConstants.UserId)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("UserId claim missing.");
                return;
            }

            if (context.Principal?.Identity?.IsAuthenticated != true)
            {
                context.Fail("Unauthenticated");
                return;
            }

            var userValidationService = context.HttpContext.RequestServices
                .GetRequiredService<IUserValidationService>();

            var validationContext = new UserValidationContext
            {
                Scenario = UserValidationScenario.AccessToken,
                Principal = context.Principal,
                ValidatePermissions = isAdmin
            };

            var result = await userValidationService.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "JWT validation failed. Error={Error}, Path={Path}",
                    result.Error,
                    context.Request.Path);

                context.Fail(result.Error.ToString());
                return;
            }

            context.HttpContext.SetUserValidationContext(validationContext);

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

            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");

            var isHtmlRequest =
                HttpMethods.IsGet(request.Method) &&
                request.Headers.Accept.Any(x =>
                    x?.Contains("text/html",
                        StringComparison.OrdinalIgnoreCase) == true);

            if (!isHtmlRequest)
                return Task.CompletedTask;

            context.HandleResponse();

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
