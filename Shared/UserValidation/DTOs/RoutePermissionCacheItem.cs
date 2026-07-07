using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.DTOs
{
    public sealed class RoutePermissionCacheItem
    {
        public int PermissionId { get; init; }

        public string PermissionCode { get; init; } = "";
        public string Route { get; init; } = "";

        public string HttpMethod { get; init; } = "";
    }
}
