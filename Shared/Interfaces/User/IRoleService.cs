using Shared.Data.Entities.Identity;
using Shared.DTOs.RolePermisison;
using Shared.Responses;

namespace Shared.Interfaces.User
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleCardDto>> GetRoleCardsAsync(
            CancellationToken cancellationToken = default);

        Task<AppRole?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> CreateAsync(CreateRoleDto dto, CancellationToken cancellationToken = default);

        Task<ServiceResult> UpdateAsync(UpdateRoleDto dto, int? currentUserLevel, CancellationToken cancellationToken = default);

        Task<ServiceResult> DeleteAsync(
    string roleId,
    int? currentUserLevel,
    CancellationToken cancellationToken = default);

        Task<ServiceResult<object>> GetPermissionOptionsAsync(
    string roleId,
    CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdatePermissionsAsync(
    UpdateRolePermissionsDto dto,
    int? currentUserLevel,
    string? assignedById,
    CancellationToken cancellationToken = default);

        Task<ServiceResult<RoleMembersDto>> GetRoleMembersAsync(
    string id,
    CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<AvailableRoleMemberDto>>> GetAvailableRoleMembersAsync(
    string id,
    string? keyword,
    CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> AssignMembersAsync(
    AssignRoleMembersDto dto,
    int? currentUserLevel,
    CancellationToken cancellationToken = default);

        Task<ServiceResult> RemoveMemberAsync(
    string roleId,
    string userId,
    int? currentUserLevel,
    CancellationToken cancellationToken = default);
    }
}
