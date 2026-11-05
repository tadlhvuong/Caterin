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
        private const string LogPrefix = "[Permission]";
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser,
            IRoutePermissionCache routeCache, IPermissionService permissionService,  ILogSafetyService logSafetyService)
        {
            var path = context.Request.Path;

            if (!path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                _logger.LogWarning("{Prefix} Endpoint not found. Path={Path}", LogPrefix, path);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                _logger.LogDebug("{Prefix} AllowAnonymous => {Path}", LogPrefix, context.Request.Path);
                await _next(context);
                return;
            }

            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                _logger.LogWarning("{Prefix} Endpoint is not RouteEndpoint. Path={Path}", LogPrefix, path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }

            var route = routeEndpoint.RoutePattern.RawText ?? "";
            var method = context.Request.Method;
           
            await routeCache.EnsureInitializedAsync();
            if (!routeCache.TryGetPermission(route, method, out var permission))
            {
                _logger.LogError("{Prefix} Route mapping NOT FOUND | {Method} {Route}", LogPrefix, method, route);

                await logSafetyService.TryLogSecurityAsync(SecurityActionType.PermissionDenied, false, $"Route '{route}' has no permission mapping.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                return;
            }
            var validationContext = context.GetUserValidationContext();
            var snapshot = validationContext?.PermissionSnapshot;
            if (snapshot == null)
            {
                _logger.LogError(
                    "{Prefix} PermissionSnapshot is null. User={User}, Path={Path}",
                    LogPrefix, currentUser.UserName, path);

                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;
                return;
            }
            if (snapshot.IsRoot)
            {
                _logger.LogInformation("{Prefix} ROOT user => Skip permission check. User={User}", LogPrefix, currentUser.UserName);
                await _next(context);
                return;
            }
            if (!snapshot.PermissionIds.Contains(permission.PermissionId))
            {
                _logger.LogWarning(
                    "{Prefix} Access denied. User={User}, Permission={Permission}, Route={Route}",
                    LogPrefix,
                    currentUser.UserName,
                    permission.PermissionCode,
                    route);

                await logSafetyService.TryLogSecurityAsync(SecurityActionType.PermissionDenied, false,
                    $"Permission denied. User={currentUser.UserName}, " +
                    $"Permission={permission.PermissionCode}, " +
                    $"Method={method}, Route={route}");

                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;
                return;
            }
            _logger.LogDebug("{Prefix} Access granted. User={User}, Permission={Permission}",
                LogPrefix, currentUser.UserName, permission.PermissionCode);

            await _next(context);
        }
    }
}
