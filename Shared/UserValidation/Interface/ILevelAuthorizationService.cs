using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Interface
{
    public interface ILevelAuthorizationService
    {
        Task EnsureCanManageUserAsync(
        string targetUserId,
        bool allowSelf = false,
        CancellationToken cancellationToken = default);

        Task EnsureCanManageRoleAsync(
            string roleName,
            CancellationToken cancellationToken = default);

        Task<int?> GetUserLevelAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task EnsureCanCreateRoleAsync(
    int newRoleLevel,
    CancellationToken cancellationToken = default);
        Task EnsureCanManageRoleLevelAsync(
    int targetRoleLevel,
    CancellationToken cancellationToken = default);
    }
}
