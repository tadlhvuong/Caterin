using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Inventory;

namespace Shared.Configurations.Inventory
{
    public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.WarehouseId).IsRequired();

            builder.HasOne(x => x.Warehouse).WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ProductVariantId).IsRequired();

            builder.HasOne(x => x.ProductVariant).WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Type).IsRequired().HasConversion<int>();

            builder.Property(x => x.Quantity).IsRequired();

            builder.Property(x => x.ReferenceId).IsRequired(false);

            builder.Property(x => x.Note).HasMaxLength(500).IsRequired(false);

            builder.Property(x => x.CreatedBy).IsRequired(false);

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.HasIndex(x => new
            {
                x.ProductVariantId,
                x.WarehouseId
            });

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
