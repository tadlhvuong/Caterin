using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;
using Shared.Enums;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("ProductCategories");


        builder.HasKey(x => new
    {
        x.Id
    });

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(x => x.Description)
            .HasMaxLength(500);
        builder.Property(x => x.SeoTitle)
            .HasMaxLength(180);

        builder.Property(x => x.SeoDescription)
            .HasMaxLength(320);

        builder.Property(x => x.SeoKeywords)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);
        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone");

        // Slug unique
        builder.HasIndex(x => x.Slug)
            .IsUnique().HasFilter("\"IsDeleted\" = false"); ;

        // SEO / filtering
        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.DisplayOrder);
    }
}