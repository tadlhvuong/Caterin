using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Identity;

namespace Shared.Interfaces.IdentityServices
{
    public interface IMenuService
    {
        Task<List<MenuDto>> GetMenusAsync();
        //Task<string?> GetPermissionByUrlAsync(string path);
    }
}
