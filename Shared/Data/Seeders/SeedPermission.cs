using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Seeders
{
    public static class SeedPermission
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            var systemModule = await dbContext.CMSModules.FirstOrDefaultAsync(x => x.Code == "system" &&
            x.IsActive);

            if (systemModule == null)
            {
                throw new Exception("Module 'system' not found.");
            }
            var permissions = new[]
            {
                    new Permission
                    {
                        ModuleId = systemModule.Id,
                        Code = PermissionCodes.SystemRoot,
                        Action = "Root",
                        Name = "System Root",
                        Description = "Full system access",
                        IsSystem = true,
                        IsActive = true
                    },

                    new Permission
                    {
                        ModuleId = systemModule.Id,
                        Code = PermissionCodes.AdminAccess,
                        Action = "Access",
                        Name = "Admin Access",
                        Description = "Allow access to admin area",
                        IsSystem = true,
                        IsActive = true
                    }
                };

            var existingCodes = new HashSet<string>
                (await dbContext.Permissions.Select(x => x.Code).ToListAsync(), StringComparer.OrdinalIgnoreCase);


            var newPermissions = permissions.Where(x => !existingCodes.Contains(x.Code)).ToList();

            if (newPermissions.Count == 0)
            {
                return;
            }

            await dbContext.Permissions.AddRangeAsync(newPermissions);

            await dbContext.SaveChangesAsync();
        }
    }
}
