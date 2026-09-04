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
    public sealed class InventoryTransactionConfiguration
    : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions");

            // =========================
            // Primary Key
            // =========================

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            // =========================
            // Warehouse
            // =========================

            builder.Property(x => x.WarehouseId)
                .IsRequired();

            builder.HasOne(x => x.Warehouse)
                .WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Product Variant
            // =========================

            builder.Property(x => x.ProductVariantId)
                .IsRequired();

            builder.HasOne(x => x.ProductVariant)
                .WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Transaction Type
            // =========================

            builder.Property(x => x.Type)
                .IsRequired()
                .HasConversion<int>();

            // =========================
            // Quantity
            // =========================

            builder.Property(x => x.Quantity)
                .IsRequired();

            // =========================
            // Reference
            // =========================

            builder.Property(x => x.ReferenceId)
                .IsRequired(false);

            // =========================
            // Note
            // =========================

            builder.Property(x => x.Note)
                .HasMaxLength(500)
                .IsRequired(false);

            // =========================
            // Created By
            // =========================

            builder.Property(x => x.CreatedBy)
                .IsRequired(false);

            // =========================
            // Created At
            // =========================

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // =========================
            // Indexes
            // =========================

            builder.HasIndex(x => new
            {
                x.ProductVariantId,
                x.WarehouseId
            });

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
