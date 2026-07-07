using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Seeders
{
    public static class SeedCurrency
    {
        public static async Task SeedAsync(AppDbContext _dbContext)
        {
            if (await _dbContext.Currencies.AnyAsync())
                return;

            await _dbContext.Currencies.AddRangeAsync(
            [
                new Currency
            {
                Code = "VND",
                Name = "Vietnamese Dong",
                Symbol = "₫",
                IsDefault = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Currency
            {
                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
            ]);

            await _dbContext.SaveChangesAsync();
        }
    }
}
