using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Interfaces.AuthServices;

namespace Shared.Data.Seeders
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var serviceProvider = scope.ServiceProvider;
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var moduleService = serviceProvider.GetRequiredService<IModuleService>();
            var permissionService = serviceProvider.GetRequiredService<IPermissionService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            await SeedLanguage.SeedAsync(dbContext);

            await SeedCurrency.SeedAsync(dbContext);

            await SeedSetting.SeedAsync(dbContext);

            await SeedWarehouse.SeedAsync(dbContext);

            await SeedCatalogs.SeedAsync(dbContext);

            await SeedModules.SeedAsync(dbContext);

            await SeedPermission.SeedAsync(dbContext);

            await permissionService.SyncPermissionsAsync();

            await SeedRoles.SeedAsync(roleManager);

            await SeedUsers.SeedAsync(userManager, configuration);

            await SeedUserRoles.SeedAsync(userManager, roleManager);

            await SeedRolePermissions.SeedAsync(dbContext, roleManager);

        }
    }
}

