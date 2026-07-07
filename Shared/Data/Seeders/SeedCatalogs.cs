using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Enums;
public static class SeedCatalogs
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.CMSCatalogs.AnyAsync())
            return;

        var catalogs = new[]
        {
        new CMSCatalog
        {
            Code = "view",
            Name = "View",
            Type = CatalogType.Action,
            IsSystem = true
        },

        new CMSCatalog
        {
            Code = "create",
            Name = "Create",
            Type = CatalogType.Action,
            IsSystem = true
        },

        new CMSCatalog
        {
            Code = "edit",
            Name = "Edit",
            Type = CatalogType.Action,
            IsSystem = true
        },

        new CMSCatalog
        {
            Code = "delete",
            Name = "Delete",
            Type = CatalogType.Action,
            IsSystem = true
        }
    };

        db.CMSCatalogs.AddRange(catalogs);

        await db.SaveChangesAsync();
    }
}
