using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity;

public static class SeedRoles
{
    public static async Task SeedAsync(RoleManager<AppRole> roleManager)
    {
        var roles = new List<AppRole>
    {
        new AppRole
        {
            Name = "System",
            NormalizedName = "SYSTEM",
            IsSystem = true,
            Level = 1000
        },
        new AppRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            IsSystem = false,
            Level = 500
        },
        new AppRole
        {
            Name = "User",
            NormalizedName = "USER",
            IsSystem = false,
            Level = 100
        }
    };

        foreach (var role in roles)
        {
            var exists = await roleManager.RoleExistsAsync(role.Name);
            if (exists) continue;

            await roleManager.CreateAsync(role);
        }
    }
}
