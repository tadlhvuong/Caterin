using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Inventory;

namespace Shared.Configurations.Inventory
{
    public class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
    {
        public void Configure(EntityTypeBuilder<InventoryStock> builder)
        {
            builder.ToTable("InventoryStocks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.WarehouseId).IsRequired();

            builder.HasOne(x => x.Warehouse).WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ProductVariantId).IsRequired();

            builder.HasOne(x => x.ProductVariant).WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.AvailableQuantity).IsRequired();

            builder.Property(x => x.ReservedQuantity).IsRequired();

            builder.Property(x => x.MinStock).IsRequired();

            builder.Property(x => x.CreateAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.HasIndex(x => new
            {
                x.WarehouseId,
                x.ProductVariantId
            }).IsUnique();
        }
    }
}
