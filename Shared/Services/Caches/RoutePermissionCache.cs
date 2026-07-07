using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Interfaces.IdentityServices;
using Shared.UserValidation.DTOs;

namespace Shared.Services.Caches
{
    public sealed class RoutePermissionCache : IRoutePermissionCache
    {
        private const string CacheKey =  "ROUTE_PERMISSION_CACHE";

        private readonly IMemoryCache _cache;
        private readonly AppDbContext _db;
        private readonly ILogger<RoutePermissionCache> _logger;

        public RoutePermissionCache(
            IMemoryCache cache,
            AppDbContext db,
            ILogger<RoutePermissionCache> logger)
        {
            _cache = cache;
            _db = db;
            _logger = logger;
        }
        public bool TryGetPermission(string route, string method, out RoutePermissionCacheItem permission)
        {
            permission = default!;

            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, RoutePermissionCacheItem>? routes))
            {
                throw new InvalidOperationException("RoutePermissionCache not initialized");
            }

            return routes.TryGetValue(CacheKeys.RoutePermission(method, route), out permission);
        }
        public async Task ReloadAsync()
        {
            var data = await _db.RoutePermissions.AsNoTracking().Include(x => x.Permission)
                .Where(x => x.IsActive).ToDictionaryAsync(
                    x => CacheKeys.RoutePermission(x.HttpMethod, x.Route),
                    x => new RoutePermissionCacheItem
                    {
                        PermissionId = x.PermissionId,
                        PermissionCode = x.Permission.Code,
                        HttpMethod = x.HttpMethod,
                        Route = x.Route,
                    });
            _cache.Set(CacheKey, data);
        }
    }
}
