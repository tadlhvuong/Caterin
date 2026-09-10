using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Inventory;

namespace Shared.Configurations.Inventory
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

            builder.Property(x => x.Address).HasMaxLength(500);

            builder.Property(x => x.Phone).HasMaxLength(30);

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.HasMany(x => x.InventoryStocks).WithOne(x => x.Warehouse)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.InventoryTransactions).WithOne(x => x.Warehouse)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
