using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

public class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        // =========================
        // Primary Key
        // =========================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // =========================
        // Product
        // =========================

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Variant information
        // =========================

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Barcode)
            .HasMaxLength(50);

        // =========================
        // Pricing
        // =========================

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.CompareAtPrice)
            .HasPrecision(18, 2);

        // =========================
        // Inventory
        // =========================

        builder.HasMany(x => x.InventoryStocks)
     .WithOne(x => x.ProductVariant)
     .HasForeignKey(x => x.ProductVariantId)
     .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Display / Status
        // =========================

        builder.Property(x => x.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // =========================
        // Audit
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // =========================
        // Indexes
        // =========================

        builder.HasIndex(x => x.Sku)
            .IsUnique();

        builder.HasIndex(x => x.Barcode)
            .IsUnique()
            .HasFilter("\"Barcode\" IS NOT NULL");

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.DisplayOrder
        });

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");
    }
}