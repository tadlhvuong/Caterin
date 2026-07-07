using Microsoft.AspNetCore.Http;
using Shared.Interfaces;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.IdentityServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services
{
    using Shared.Constants.Permission;
    using System.Security.Claims;

    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClaimsPrincipal? _user;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _user = httpContextAccessor.HttpContext?.User;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserName => User?.FindFirstValue(ClaimConstants.UserName);

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        public IReadOnlyList<string> Roles =>
            User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

        //public string? RoleId
        //{
        //    get
        //    {
        //        var value =
        //            User?.FindFirstValue(ClaimConstants.RoleId);

        //        return value;
        //    }
        //}

        //public int RoleLevel
        //{
        //    get
        //    {
        //        var value = User?.FindFirstValue(ClaimConstants.RoleLevel);

        //        return int.TryParse(
        //            value,
        //            out var result)
        //                ? result
        //                : 0;
        //    }
        //}

        //public HashSet<int> PermissionIds
        //{
        //    get
        //    {
        //        var permissions =
        //            User?.FindFirstValue(ClaimConstants.Permissions);

        //        if (string.IsNullOrWhiteSpace(
        //            permissions))
        //        {
        //            return new HashSet<int>();
        //        }

        //        return permissions
        //            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //            .Select(int.Parse)
        //            .ToHashSet();
        //    }
        //}
    }
}
