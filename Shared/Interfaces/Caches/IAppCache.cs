using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Caches
{
    public interface IAppCache
    {
        //Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
        Task<T?> GetOrCreateAsync<T>(string key,Func<Task<T>> factory,TimeSpan? ttl = null);
        Task RemoveAsync(string key);
    }
}
