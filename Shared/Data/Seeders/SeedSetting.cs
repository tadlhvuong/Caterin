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
                Group = Enums.SettingGroup.General,
                Description = "Website name",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.language",
                Value = "vi-VN",
                Group = Enums.SettingGroup.General,
                Description = "Default language",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.currency",
                Value = "VND",
                Group = Enums.SettingGroup.General,
                Description = "Default currency",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            },

            new Setting
            {
                Key = "site.timezone",
                Value = "Asia/Ho_Chi_Minh",
                Group = Enums.SettingGroup.General,
                Description = "Default timezone",
                IsPublic = true,
                CreatedAt = DateTime.UtcNow
            }
            ]);

            await _dbContext.SaveChangesAsync();
        }
    }
}
