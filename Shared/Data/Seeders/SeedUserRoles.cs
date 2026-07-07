using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity;

namespace Shared.Data.Seeders
{
    public static class SeedUserRoles
    {
        public static async Task SeedAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            var user =
                await userManager.FindByNameAsync("root");

            if (user == null)
                return;

            if (await userManager.IsInRoleAsync(user, "System"))
                return;

            await userManager.AddToRoleAsync(user, "System");
        }
    }
    
}
