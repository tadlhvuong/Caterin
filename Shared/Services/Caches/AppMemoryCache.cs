using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Interfaces.Caches;
using Shared.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Caches
{
    public class AppMemoryCache : IAppCache
    {
        private readonly IMemoryCache _cache;

        public AppMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            if (_cache.TryGetValue(key, out T value))
                return value;

            var result = await factory();

            _cache.Set(key, result, ttl ?? TimeSpan.FromMinutes(30));
           
            return result;
        }


        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
