using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
public static class SeedModules
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var modules = new[]
        {
            new CMSModule
            {
                Code = "system",
                Name = "system",
                IsSystem = true
            },
            new CMSModule
            {
                Code = "dashboard",
                Name = "Dashboard",
                IsSystem = false
            },

            new CMSModule
            {
                Code = "users",
                Name = "Users",
                IsSystem = false
            },

            new CMSModule
            {
                Code = "roles",
                Name = "Roles",
                IsSystem = false
            },

            new CMSModule
            {
                Code = "permissions",
                Name = "Permissions",
                IsSystem = false
            },

            new CMSModule
            {
                Code = "modules",
                Name = "Modules",
                IsSystem = false
            },

            new CMSModule
            {
                Code = "products",
                Name = "Products",
                IsSystem = false
            }
        };

        var existingModules = await db.CMSModules.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            if (existingModules.TryGetValue(module.Code, out var existing))
            {
                existing.Name = module.Name;
                existing.IsSystem = module.IsSystem;
            }
            else
            {
                await db.CMSModules.AddAsync(module);
            }
        }

        await db.SaveChangesAsync();
    }
}
