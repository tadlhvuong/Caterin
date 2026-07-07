using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.IdentityServices
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }

        string? UserId { get; }

        string? UserName { get; }

        IReadOnlyList<string> Roles { get; }

        //string? RoleId { get; }

        //int RoleLevel { get; }

        //HashSet<int> PermissionIds { get; }
    }
}
