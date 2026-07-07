using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.DTOs.Identity;
using Shared.Interfaces.IdentityServices;

namespace Shared.Services
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Shared.Extensions;
    using Shared.Interfaces.AuthServices;
    using Shared.Services.Authentication;
    using Shared.UserValidation.DTOs;

    public sealed class MenuService : IMenuService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserService _currentUser;
        private readonly IPermissionService _permissionService;
        

        public MenuService(AppDbContext db,
            ICurrentUserService currentUser, IPermissionService permissionService )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async Task<List<MenuDto>> GetMenusAsync()
        {
            var validationContext = _httpContextAccessor.HttpContext?.GetUserValidationContext();

            var snapshot = validationContext?.PermissionSnapshot;

            if (snapshot == null)
            {
                return new();
            }

            var menus = await _db.Menus
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            menus = menus.Where(menu =>
                    menu.PermissionId == null
                    || snapshot.IsRoot
                    || snapshot.PermissionIds.Contains(menu.PermissionId.Value))
                .ToList();

            return BuildTree(menus, null);
        }
        private List<MenuDto> BuildTree(List<Menu> menus, long? parentId)
        {
            return menus
                .Where(x => x.ParentId == parentId)
                .Select(x => new MenuDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = x.Url,
                    Icon = x.Icon,

                    Children = BuildTree(
                        menus,
                        x.Id)
                })
                .ToList();
        }
    }
}
