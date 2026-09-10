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
        /// <summary>
        /// Đảm bảo RoutePermission cache chỉ được khởi tạo đúng 1 lần, và chỉ có 1 request vào 
        /// </summary>
        /// <returns></returns>
        Task EnsureInitializedAsync();
        /// <summary>
        /// Check route + HTTP method có được khai báo permission trong cache hay không
        /// </summary>
        /// <param name="route"></param>
        /// <param name="method"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        bool TryGetPermission(string route, string method, out RoutePermissionCacheItem permission);
        /// <summary>
        /// Mapping Route → Permission từ database và xây dựng lại cache
        /// </summary>
        /// <returns></returns>
        Task ReloadAsync();
    }
}
