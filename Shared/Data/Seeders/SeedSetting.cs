using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Seeders
{
    public static class SeedSetting
    {
        public static async Task SeedAsync(AppDbContext _dbContext)
        {
            if (await _dbContext.Settings.AnyAsync())
                return;

            await _dbContext.Settings.AddRangeAsync(
            [
                new Setting
            {
                Key = "site.name",
                Value = "Caterin",
                Description = "Website name",
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.language",
                Value = "vi-VN",
                Description = "Default language",
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.currency",
                Value = "VND",
                Description = "Default currency",
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.timezone",
                Value = "Asia/Ho_Chi_Minh",
                Description = "Default timezone",
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            }
            ]);

            await _dbContext.SaveChangesAsync();
        }
    }
}
