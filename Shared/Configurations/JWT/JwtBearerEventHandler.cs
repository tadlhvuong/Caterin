using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Constants.Permission;
using Shared.Extensions;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;

namespace Shared.Configurations.JWT
{

    // JwtBearerHandler call run (framework nội bộ ASP.NET Core)
    //TokenValidated: JwtBearerHandler call
    //AuthenticationFailed: Jwt middleware cal
    //Challenge: Authorization middleware call
    //Forbidden: Authorization middleware call
    public sealed class JwtBearerEventHandler : JwtBearerEvents
    {
        private readonly ILogger<JwtBearerEventHandler> _logger;

        public JwtBearerEventHandler(
            IUserValidationService validationService,
            ILogger<JwtBearerEventHandler> logger)
        {
            _logger = logger;
        }
        public override Task MessageReceived(MessageReceivedContext context)
        {
            var token = context.Request.Cookies["access_token"];
            Console.WriteLine($"Token exists: {!string.IsNullOrWhiteSpace(token)}");
            context.Token = token;
            return Task.CompletedTask;
        }
        public override async Task TokenValidated(
            TokenValidatedContext context)
        {
            var path = context.HttpContext.Request.Path;

            // Chỉ kiểm tra cho Admin
            if (!path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var userId = context.Principal?
                .FindFirst(ClaimConstants.UserId)?
                .Value;

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
            var userValidationService = context.HttpContext.RequestServices.GetRequiredService<IUserValidationService>();

            var validationContext = new UserValidationContext
            {
                Scenario = UserValidationScenario.AccessToken,
                Principal = context.Principal
            };

            var result = await userValidationService.ValidateAsync(validationContext);

            if (!result.Succeeded)
            {
                _logger.LogWarning("JWT validation failed. Error={Error}", result.Error);

                context.Fail(result.Error.ToString());
                return;
            }
            context.HttpContext.SetUserValidationContext(validationContext);
            Console.WriteLine("Token validated");
            await base.TokenValidated(context);
        }

        public override Task AuthenticationFailed(
            AuthenticationFailedContext context)
        {
            _logger.LogWarning(
                context.Exception,
                "JWT authentication failed.");

            return Task.CompletedTask;
        }

        public override Task Challenge(
            JwtBearerChallengeContext context)
        {
            _logger.LogDebug(
                "Unauthorized request: {Path}",
                context.Request.Path);

            return Task.CompletedTask;
        }

        public override Task Forbidden(
            ForbiddenContext context)
        {
            _logger.LogWarning(
                "Forbidden request: {Path}",
                context.Request.Path);

            return Task.CompletedTask;
        }
    }
}
