using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;

namespace Shared.Middlewares
{
    public sealed class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser,
            IRoutePermissionCache routeCache, IPermissionService permissionService, 
            ISecurityLogger securityLogger)
        {
            if (!context.Request.Path.StartsWithSegments("/admin"))
            {
                await _next(context);
                return;
            }

            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                await _next(context);
                return;
            }

            var route = routeEndpoint.RoutePattern.RawText ?? "";

            var method = context.Request.Method;

            //if (!routeCache.TryGetPermissionId(route, method, out var permissionId))
            if (!routeCache.TryGetPermission(route, method, out var permission))
            {
                await securityLogger.LogAsync(SecurityActionType.PermissionDenied, false, 
                    $"Route '{route}' has no permission mapping.");
                _logger.LogError($"Route '{route}' has no permission mapping.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                return;
            }

            var validationContext = context.GetUserValidationContext();

            if (validationContext?.PermissionSnapshot == null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }
            var snapshot = validationContext.PermissionSnapshot;

            if (snapshot.IsRoot)
            {
                await _next(context);
                return;
            }
            //if (!snapshot.PermissionIds.Contains(permissionId))
            if (!snapshot.PermissionIds.Contains(permission.PermissionId))
            {
                await securityLogger.LogAsync(SecurityActionType.PermissionDenied, false,
                     $"Permission denied. User={currentUser.UserName}, Permission={permission.PermissionCode}, method={permission.HttpMethod}, nameRoute={ permission.Route}");

                _logger.LogError($"Permission denied: User ={ currentUser.UserName}, Permission ={ permission.PermissionCode}, method={permission.HttpMethod}, nameRoute={permission.Route}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await _next(context);
        }
    }
}
