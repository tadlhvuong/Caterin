using Microsoft.EntityFrameworkCore;
using Shared.Constants.Core;
using Shared.Data.Context;
using Shared.Data.Entities.Inventory;

namespace Shared.Data.Seeders
{
    public static class SeedWarehouse
    {
        public static async Task SeedAsync(AppDbContext _dbContext)
        {
            var mainWarehouse = await _dbContext.Warehouses .FirstOrDefaultAsync(x => x.Id == WarehouseConstants.MainWarehouseId);

            if (mainWarehouse == null)
            {
                _dbContext.Warehouses.Add(new Warehouse
                {
                    Id = WarehouseConstants.MainWarehouseId,
                    Name = "Tổng kho",
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
