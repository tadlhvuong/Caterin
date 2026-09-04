using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Inventory
{
    public class InventoryStockConfiguration
    : IEntityTypeConfiguration<InventoryStock>
    {
        public void Configure(EntityTypeBuilder<InventoryStock> builder)
        {
            builder.ToTable("InventoryStocks");

            // =========================
            // Primary Key
            // =========================

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            // =========================
            // Warehouse
            // =========================

            builder.Property(x => x.WarehouseId).IsRequired();

            builder.HasOne(x => x.Warehouse)
                .WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Product Variant
            // =========================

            builder.Property(x => x.ProductVariantId)
                .IsRequired();

            builder.HasOne(x => x.ProductVariant)
                .WithMany(x => x.InventoryStocks)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Quantity
            // =========================

            builder.Property(x => x.AvailableQuantity)
                .IsRequired();

            builder.Property(x => x.ReservedQuantity)
                .IsRequired();

            builder.Property(x => x.MinStock)
                .IsRequired();

            // =========================
            // UpdatedAt
            // =========================

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // =========================
            // Unique
            // =========================
            // Một ProductVariant chỉ có
            // một stock record trong một Warehouse.

            builder.HasIndex(x => new
            {
                x.WarehouseId,
                x.ProductVariantId
            })
            .IsUnique();
        }
    }
}
