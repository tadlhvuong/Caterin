using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Seeders
{
    public static class SeedLanguage
    {
        public static async Task SeedAsync(AppDbContext _dbContext)
        {
            if (await _dbContext.Languages.AnyAsync())
                return;

            await _dbContext.Languages.AddRangeAsync(
            [
                new Language
            {
                Code = "vi-VN",
                Name = "Vietnamese",
                NativeName = "Tiếng Việt",
                IsDefault = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Code = "en-US",
                Name = "English",
                NativeName = "English",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
            ]);

            await _dbContext.SaveChangesAsync();
        }
    }
}
