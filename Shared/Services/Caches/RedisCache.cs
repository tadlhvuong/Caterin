using Microsoft.Extensions.Caching.Distributed;
using Shared.Interfaces.Caches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Services.Caches
{
    public class RedisCache : IAppCache
    {
        private readonly IDistributedCache _cache;

        public RedisCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? ttl = null)
        {
            var cached = await _cache.GetStringAsync(key);

            if (cached != null)
                return JsonSerializer.Deserialize<T>(cached);

            var result = await factory();

            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                        ?? TimeSpan.FromMinutes(30)
                });

            return result;
        }

        public Task RemoveAsync(string key)
            => _cache.RemoveAsync(key);
    }
}
