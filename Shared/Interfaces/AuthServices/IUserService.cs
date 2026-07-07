using Shared.Data.Entities.Identity;

namespace Shared.Interfaces.AuthServices
{
    public interface IUserService
    {
        #region Query

        Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
        Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

        #endregion

        #region Role

        Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

        Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

        Task ReplaceRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

        #endregion

        #region Permission

        Task IncreasePermissionVersionAsync(string userId, CancellationToken cancellationToken = default);

        Task IncreasePermissionVersionByRoleAsync(string roleId, CancellationToken cancellationToken = default);

        #endregion

        #region User Status
        Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);

        Task LockAsync(string userId, CancellationToken cancellationToken = default);

        Task UnlockAsync(string userId, CancellationToken cancellationToken = default);

        Task EnableAsync(string userId, CancellationToken cancellationToken = default);

        Task DisableAsync(string userId, CancellationToken cancellationToken = default);

        #endregion
    }
}
