using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

namespace Shared.Configurations.Product
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.ToTable("ProductVariants");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.ProductId).IsRequired();

            builder.HasOne(x => x.Product).WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

            builder.Property(x => x.Sku).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Barcode).HasMaxLength(50);

            builder.Property(x => x.Price).IsRequired().HasPrecision(18, 2);

            builder.Property(x => x.CompareAtPrice).HasPrecision(18, 2);

            builder.HasMany(x => x.InventoryStocks).WithOne(x => x.ProductVariant)
                .HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);

            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.HasIndex(x => new
            {
                x.ProductId,
                x.Sku
            }).IsUnique().HasFilter("\"IsActive\" = true");
            
            builder.HasIndex(x => x.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");

            builder.HasIndex(x => new
            {
                x.ProductId,
                x.DisplayOrder
            });

            builder.HasIndex(x => x.ProductId).IsUnique().HasFilter("\"IsDefault\" = true");
        }
    }
}