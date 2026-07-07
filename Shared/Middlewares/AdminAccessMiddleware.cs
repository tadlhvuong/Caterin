using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.UserValidation.Interface;

namespace Shared.Middlewares
{
    public class AdminAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminAccessMiddleware> _logger;

        public AdminAccessMiddleware(RequestDelegate next, ILogger<AdminAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, 
            ICurrentUserService currentUser, IUserValidationService userValidationService,
            ISecurityLogger securityLogger)
        {
            var path = context.Request.Path;

            if (!path.StartsWithSegments("/admin"))
            {
                await _next(context);
                return;
            }


            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                if (path.StartsWithSegments("/admin/auth"))
                {
                    await _next(context);
                    return;
                }
                context.Response.Redirect("/admin/auth/login");
                return;
            }

            var userId = currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) 
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized; 
                return; 
            }

            var validationContext = context.GetUserValidationContext();
            if (validationContext == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (!await userValidationService.HasAdminAccessAsync(validationContext))
            {
                await securityLogger.LogAsync(SecurityActionType.AdminAccessDenied, false,
                    "User tried to access Admin area.");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                _logger.LogWarning("User {UserId} tried to access Admin area ", userId);
                return;
            }

            _logger.LogInformation("User {UserId} login success", userId);
            await _next(context);
        }
    }
}
