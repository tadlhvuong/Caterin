
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Interfaces.IdentityServices;
using System.Reflection;

namespace Shared.Services.Authentication
{
    public class RoutePermissionService : IRoutePermissionService
    {
        private readonly EndpointDataSource _endpointDataSource;
        private readonly AppDbContext _dbContext;
        private readonly IRoutePermissionCache _routePermissionCache;
        private readonly ILogger<RoutePermissionService> _logger;

        public RoutePermissionService(
            EndpointDataSource endpointDataSource,
            AppDbContext dbContext,
            IRoutePermissionCache routePermissionCache, ILogger<RoutePermissionService> logger)
        {
            _endpointDataSource = endpointDataSource;
            
            _dbContext = dbContext;
            _routePermissionCache = routePermissionCache;
            _logger = logger;
        }

        public async Task SyncAsync(CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var permissions = await _dbContext.Permissions.Where(x => x.IsActive).ToListAsync(cancellationToken);

                var routePermissions = await _dbContext.RoutePermissions.AsTracking().ToListAsync();

                var permissionLookup = permissions.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

                var routeLookup = routePermissions.ToDictionary(x => BuildRouteKey(
                        x.HttpMethod, x.Route), StringComparer.OrdinalIgnoreCase);

                var scannedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var endpoints = _endpointDataSource.Endpoints.OfType<RouteEndpoint>();

                foreach (var endpoint in endpoints)
                {
                    ProcessEndpoint(endpoint, permissionLookup, routeLookup, scannedRoutes);
                }

                DisableRemovedRoutes(routeLookup, scannedRoutes);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await _routePermissionCache.ReloadAsync();

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "RoutePermission Sync Failed");
                await transaction.RollbackAsync();
                throw;
            }
        }
        private bool IsAdminController( ControllerActionDescriptor actionDescriptor)
        {
            return string.Equals( actionDescriptor.RouteValues["area"], "Admin", StringComparison.OrdinalIgnoreCase);
        }
        private void ProcessEndpoint( RouteEndpoint endpoint,
            Dictionary<string, Permission> permissionLookup,
            Dictionary<string, RoutePermission> routeLookup,
            HashSet<string> scannedRoutes)
        {

            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();

            if (actionDescriptor == null || !IsAdminController(actionDescriptor))
                return;

            var moduleAttribute = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<PermissionModuleAttribute>();
            if (moduleAttribute == null)
            {
                return;
            }

            var permissionActionAttribute = endpoint.Metadata.GetMetadata<PermissionActionAttribute>();

            if (permissionActionAttribute == null)
            {
                throw new InvalidOperationException($"Route '{actionDescriptor.ControllerName}/{actionDescriptor.ActionName}' chưa khai báo PermissionActionAttribute.");
            }


            var module = moduleAttribute.Module.ToLower();
            var action =  permissionActionAttribute.Action.ToString().ToLower();

            var permissionCode = $"{module}.{action}";
            if (!permissionLookup.TryGetValue(permissionCode, out var permission))
            {
                _logger.LogWarning("Permission '{PermissionCode}' not found for route {Route}",
                    permissionCode, endpoint.RoutePattern.RawText);
                return;
            }

            var httpMethod = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";

            var route = endpoint.RoutePattern.RawText ?? string.Empty;

            var routeKey = BuildRouteKey(httpMethod, route);

            scannedRoutes.Add(routeKey);

            if (routeLookup.TryGetValue(routeKey, out var existingRoute))
            {
                existingRoute.Controller = actionDescriptor.ControllerName;
                existingRoute.Action = actionDescriptor.ActionName;
                existingRoute.PermissionId = permission.Id;
                existingRoute.IsActive = true;
                existingRoute.UpdatedAt = DateTime.UtcNow;
                return;
            }

            var routePermission = new RoutePermission
                {
                    Controller = actionDescriptor.ControllerName,
                    Action = actionDescriptor.ActionName,
                    HttpMethod = httpMethod,
                    Route = route,
                    PermissionId = permission.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

            _dbContext.RoutePermissions.Add(routePermission);

            routeLookup[routeKey] = routePermission;
        }

        private void DisableRemovedRoutes(Dictionary<string, RoutePermission> routeLookup, HashSet<string> scannedRoutes)
        {
            foreach (var route in routeLookup.Values)
            {
                var routeKey = BuildRouteKey(route.HttpMethod, route.Route);
                var isActive = scannedRoutes.Contains(routeKey);

                if (route.IsActive == isActive)
                {
                    continue;
                }
                route.IsActive = isActive;
                route.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static string BuildRouteKey(string method, string route)
        {
            route = CommonHelper.NormalizeRoute(route);

            return $"{method}:{route}";
        }
    }
}
