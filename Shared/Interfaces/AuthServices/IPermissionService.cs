using Shared.Services.Authentication;

namespace Shared.Interfaces.AuthServices
{
    public interface IPermissionService
    {
        /// <summary>
        /// Tạo permission
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="saveChanges"></param>
        /// <returns></returns>
        Task GeneratePermissionsAsync(int moduleId, bool saveChanges);

        /// <summary>
        /// Đồng bộ permission
        /// </summary>
        /// <returns></returns>
        Task SyncPermissionsAsync();

        /// <summary>
        /// Get permission theo code
        /// </summary>
        /// <param name="permissionCode"></param>
        /// <returns></returns>
        Task<int?> GetPermissionIdAsync(string permissionCode);

        /// <summary>
        /// Get permission user theo snapshot
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="permissionVersion"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<UserPermissionSnapshot> GetUserPermissionSnapshotAsync(string userId, long permissionVersion, CancellationToken cancellationToken = default);
    }
}
