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
        private const string LogPrefix = "[RouteCache]";
        private const string CacheKey =  "ROUTE_PERMISSION_CACHE";
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _initialized;

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
        public async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;
            _logger.LogInformation("{Prefix} Initializing RoutePermission cache...", LogPrefix);
            await _lock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                await ReloadAsync();

                _initialized = true;
                _logger.LogInformation("{Prefix} Initialization completed.", LogPrefix);
            }
            finally
            {
                _lock.Release();
            }
        }
        public bool TryGetPermission(string route, string method, out RoutePermissionCacheItem permission)
        {
            permission = default!;

            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, RoutePermissionCacheItem>? routes))
            {
                _logger.LogError("{Prefix} Cache not initialized.", LogPrefix);
                throw new InvalidOperationException("RoutePermissionCache not initialized");
            }
            //return routes.TryGetValue(CacheKeys.RoutePermission(method, route), out permission);

            var key = CacheKeys.RoutePermission(method, route);

            _logger.LogDebug(
                "{Prefix} Lookup => {Key}",
                LogPrefix,
                key);

            if (routes.TryGetValue(key, out permission))
            {
                _logger.LogDebug(
                    "{Prefix} Cache HIT => Permission={PermissionCode} ({PermissionId})",
                    LogPrefix,
                    permission.PermissionCode,
                    permission.PermissionId);

                return true;
            }

            _logger.LogWarning(
                "{Prefix} Cache MISS => {Key}",
                LogPrefix,
                key);

            return false;
        }
        public async Task ReloadAsync()
        {
            _logger.LogInformation("{Prefix} Reloading RoutePermission cache...", LogPrefix);
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
            _logger.LogInformation("Reload rotuepermission count = {Count}", data.Count);
            foreach (var item in data)
            {
                _logger.LogInformation("Cache: {Key}", item.Key);
            }
            _cache.Set(CacheKey, data);
        }
    }
}
