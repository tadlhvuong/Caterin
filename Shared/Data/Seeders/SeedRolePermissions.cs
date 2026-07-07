using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using System.Data;

public static class SeedRolePermissions
{
    public static async Task SeedAsync(
    AppDbContext db,
    RoleManager<AppRole> roleManager)
    {
        var systemRole = await roleManager.FindByNameAsync("System");

        if (systemRole == null)
            return;

        var permissionIds = await db.Permissions
            .Select(x => x.Id)
            .ToListAsync();

        var existingPermissionIds = (await db.RolePermissions.Where(x => x.RoleId == systemRole.Id)
            .Select(x => x.PermissionId).ToListAsync()).ToHashSet();

        var newRolePermissions = permissionIds
            .Where(id => !existingPermissionIds.Contains(id))
            .Select(id => new RolePermission
            {
                RoleId = systemRole.Id,
                PermissionId = id
            })
            .ToList();

        if (newRolePermissions.Count == 0)
            return;

        db.RolePermissions.AddRange(newRolePermissions);

        await db.SaveChangesAsync();
    }
}
