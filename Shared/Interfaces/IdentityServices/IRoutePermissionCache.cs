using Shared.Data.Entities.Identity.Core;
using Shared.UserValidation.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.IdentityServices
{
    public interface IRoutePermissionCache
    {
        bool TryGetPermission(string route, string method, out RoutePermissionCacheItem permission);
        Task ReloadAsync();
    }
}
