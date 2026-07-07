using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Interfaces.AuthServices;
public static class GeneratePermissions
{
    //public static async Task SeedAsync(AppDbContext _dbContext)
    //{
    //    if (await _dbContext.Permissions.AnyAsync())
    //        return;

    //    var permissions = new[]
    //    {
    //    new Permission
    //    {
    //        Code = "dashboard.view",
    //        Name = "Dashboard View"
    //    },

    //    new Permission
    //    {
    //        Code = "users.view",
    //        Name = "Users View"
    //    },

    //    new Permission
    //    {
    //        Code = "users.create",
    //        Name = "Users Create"
    //    },

    //    new Permission
    //    {
    //        Code = "users.edit",
    //        Name = "Users Edit"
    //    },

    //    new Permission
    //    {
    //        Code = "users.delete",
    //        Name = "Users Delete"
    //    }
    //};

    //    _dbContext.Permissions.AddRange(permissions);

    //    await _dbContext.SaveChangesAsync();
    //}
    //public static async Task GeneratePermissionsAsync(AppDbContext _dbContext, IPermissionService permissionService)
    //{
    //    var moduleIds = await _dbContext.CMSModules
    //        .Select(x => x.Id)
    //        .ToListAsync();

    //    foreach (var moduleId in moduleIds)
    //    {
    //        await permissionService.GeneratePermissionsAsync(moduleId);
    //    }
    //}
}
