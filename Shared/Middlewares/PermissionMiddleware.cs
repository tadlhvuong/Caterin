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
                _logger.LogWarning("{Prefix} Endpoint not found. Path={Path}",
                LogPrefix, context.Request.Path);
                await _next(context);
                return;
            }

            if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                _logger.LogDebug("{Prefix} AllowAnonymous => {Path}",
                LogPrefix, context.Request.Path);
                await _next(context);
                return;
            }

            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                _logger.LogWarning("{Prefix} Endpoint is not RouteEndpoint. Path={Path}",
                LogPrefix, context.Request.Path);
                await _next(context);
                return;
            }

            var route = routeEndpoint.RoutePattern.RawText ?? "";

            var method = context.Request.Method;

            _logger.LogInformation(
            "{Prefix} Checking permission | {Method} {Route}",
            LogPrefix, method, route);
            await routeCache.EnsureInitializedAsync();
            if (!routeCache.TryGetPermission(route, method, out var permission))
            {
                _logger.LogError(
                "{Prefix} Route mapping NOT FOUND | {Method} {Route}",
                LogPrefix, method, route);

                await securityLogger.LogAsync(SecurityActionType.PermissionDenied, false, 
                    $"Route '{route}' has no permission mapping.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                return;
            }
            _logger.LogDebug(
           "{Prefix} Route mapped => Permission={PermissionCode} ({PermissionId})",
           LogPrefix,
           permission.PermissionCode,
           permission.PermissionId);
            var validationContext = context.GetUserValidationContext();

            if (validationContext?.PermissionSnapshot == null)
            {
                _logger.LogError(
                "{Prefix} PermissionSnapshot is NULL. User={User}",
                LogPrefix,
                currentUser.UserName);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }
            var snapshot = validationContext.PermissionSnapshot;

            if (snapshot.IsRoot)
            {
                _logger.LogInformation(
                "{Prefix} ROOT user => Skip permission check. User={User}",
                LogPrefix,
                currentUser.UserName);
                await _next(context);
                return;
            }
            _logger.LogDebug(
           "{Prefix} User={User} PermissionCount={Count}",
           LogPrefix,
           currentUser.UserName,
           snapshot.PermissionIds.Count);
            if (!snapshot.PermissionIds.Contains(permission.PermissionId))
            {
                _logger.LogWarning(
                "{Prefix} ACCESS DENIED | User={User} | Permission={Permission} | Route={Method} {Route}",
                LogPrefix,
                currentUser.UserName,
                permission.PermissionCode,
                permission.HttpMethod,
                permission.Route);
                await securityLogger.LogAsync(SecurityActionType.PermissionDenied, false,
                     $"Permission denied. User={currentUser.UserName}, Permission={permission.PermissionCode}, method={permission.HttpMethod}, nameRoute={ permission.Route}");

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            _logger.LogInformation(
          "{Prefix} ACCESS GRANTED | User={User} | Permission={Permission}",
          LogPrefix,
          currentUser.UserName,
          permission.PermissionCode);
            await _next(context);
        }
    }
}
