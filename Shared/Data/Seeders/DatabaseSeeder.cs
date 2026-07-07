using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Interfaces.AuthServices;

namespace Shared.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var serviceProvider = scope.ServiceProvider;
        var dbContext = services.GetService<AppDbContext>();
        var permissionService = serviceProvider.GetRequiredService<IPermissionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        if (dbContext == null) return;

        await SeedLanguage.SeedAsync(dbContext);

        await SeedCurrency.SeedAsync(dbContext);

        await SeedSetting.SeedAsync(dbContext);

        await SeedCatalogs.SeedAsync(dbContext);

        await SeedModules.SeedAsync(dbContext);

        await SeedPermission.SeedAsync(dbContext);

        await permissionService.SyncPermissionsAsync();

        await SeedRoles.SeedAsync( roleManager);

        await SeedUsers.SeedAsync(userManager);

        await SeedUserRoles.SeedAsync(userManager, roleManager);

        await SeedRolePermissions.SeedAsync(dbContext, roleManager);

    }
}
