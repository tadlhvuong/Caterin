using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;
using Shared.Enums;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("Products");


        // =========================
        // Primary Key
        // =========================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();


        // =========================
        // Basic information
        // =========================

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Sku)
            .HasMaxLength(100);

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasColumnType("text");


        // =========================
        // Pricing
        // =========================

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);


        // =========================
        // Shipping
        // =========================

        builder.Property(x => x.Weight)
            .HasPrecision(18, 3);

        builder.Property(x => x.WeightUnit)
            .HasConversion<int>();


        // =========================
        // Display
        // =========================

        builder.Property(x => x.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);


        // =========================
        // SEO
        // =========================

        builder.Property(x => x.SeoTitle)
            .HasMaxLength(250);

        builder.Property(x => x.SeoDescription)
            .HasMaxLength(500);

        builder.Property(x => x.CanonicalUrl)
            .HasMaxLength(500);

        builder.Property(x => x.NoIndex)
            .HasDefaultValue(false);


        // =========================
        // Business
        // =========================

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(ProductStatus.Draft);

        builder.Property(x => x.CategoryId)
            .IsRequired();


        // =========================
        // Audit
        // =========================

        builder.Property(x => x.CreatedAt) 
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.PublishedAt);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);


        // =========================
        // Relationships
        // =========================

        // Product -> Category
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product -> ProductVariant
        builder.HasMany(x => x.Variants)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);


        // Product -> ProductMedia
        builder.HasMany(x => x.ProductMedias)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);


        // =========================
        // Indexes
        // =========================

        // Slug unique
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false"); ;
        builder.HasIndex(x => x.Sku).HasFilter("\"IsDeleted\" = false"); ;

        builder.HasIndex(x => x.CategoryId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.IsFeatured);

        builder.HasIndex(x => x.IsDeleted);

        builder.HasIndex(x => new
        {
            x.Status,
            x.IsDeleted
        });

        builder.HasIndex(x => new
        {
            x.CategoryId,
            x.Status,
            x.IsDeleted
        });
    }
}