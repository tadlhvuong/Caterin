using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Caches
{

    public static class CacheKeys
    {
        public static string UserPermission(string userId) => $"perm:user:{userId}";
        public static string UserRoles(string userId) => $"user:roles:{userId}";
        public static string RoutePermission(string method, string route)
        => $"{method.ToUpperInvariant()}:{route.ToLowerInvariant()}";
        public static string PermissionCodeLookup() => $"perm:code";

        public static string UserPermissionSnapshot(string userId, long permissionVersion) => $"perm:snapshot:{userId}:{permissionVersion}";
    }
}
