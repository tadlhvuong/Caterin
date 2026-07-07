using Shared.Services.Authentication;

namespace Shared.Interfaces.AuthServices
{
    public interface IPermissionService
    {
        /// <summary>
        /// Tạo permission
        /// </summary>
        Task GeneratePermissionsAsync(int moduleId, bool saveChanges);

        /// <summary>
        /// Đồng bộ permission
        /// </summary>
        Task SyncPermissionsAsync();

        /// <summary>
        /// Lấy permission theo code
        /// </summary>
        Task<int?> GetPermissionIdAsync(string permissionCode);
        //Task<IReadOnlySet<int>> GetPermissionsAsync(string userId);

        /// <summary>
        /// Lấy permission user theo snapshot
        /// </summary>
        Task<UserPermissionSnapshot> GetUserPermissionSnapshotAsync(string userId, long permissionVersion,
        CancellationToken cancellationToken = default);
    }
}
