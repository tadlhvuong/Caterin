using Shared.Data.Entities.Identity;
using Shared.DTOs.Identity;
using Shared.Requests;
using Shared.Responses;

namespace Shared.Interfaces.AuthServices
{
    public interface IUserService
    {
        #region Query
        /// <summary>
        /// Tìm user theo Id user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Tìm user theo name user
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
        /// <summary>
        /// Tìm user theo email user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
        /// <summary>
        /// Get danh sách quyền hạn
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
        #endregion

        #region Role
        /// <summary>
        /// Gán vai trò cho user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
        /// <summary>
        /// Xóa vai trò của user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
        /// <summary>
        /// Thay đổi vai trò của user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roles"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ReplaceRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);
        #endregion

        #region Permission
        /// <summary>
        /// Tăng version quyền hạn theo id user
        /// Thay đổi quyền hạn ảnh hưởng đến user có id trùng khớp
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task IncreasePermissionVersionAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Tăng version quyền hạn theo id role
        /// Thay đổi quyền hạn ảnh hưởng đến user có id vai trò trùng khớp
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task IncreasePermissionVersionByRoleAsync(string roleId, CancellationToken cancellationToken = default);
        #endregion

        #region User Status
        /// <summary>
        /// Đổi mật khẩu: User đăng nhập được đổi mật khẩu
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
        /// <summary>
        /// Khóa tạm mật tài khoản
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LockAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Mở tài khoản đã khóa tạm
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UnlockAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Kiểm tra tài khoản có đang bị khóa không
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> IsLockedAsync(AppUser user);
        /// <summary>
        /// Kiểm tra tài khoản có đang bị khóa không theo id user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<bool> IsLockedIdAsync(string userId);
        /// <summary>
        /// Mở lại tài khoản
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task EnableAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Khóa vĩnh viễn tài khoản
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisableAsync(string userId, CancellationToken cancellationToken = default);
        #endregion User Status
        /// <summary>
        /// Get danh sách user
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagedResult<UserListResponse>> GetUsersAsync(UserQueryRequest request, CancellationToken cancellationToken = default);
    }
}
