using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

public class AttributeConfiguration : IEntityTypeConfiguration<Shared.Data.Entities.Product.Attribute>
{
    public void Configure(EntityTypeBuilder<Shared.Data.Entities.Product.Attribute> builder)
    {
        builder.ToTable("Attributes");

        builder.HasKey(x => x.Id);

        // =========================
        // Information
        // =========================

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Code must be unique
        builder.HasIndex(x => x.Code)
            .IsUnique();

        // =========================
        // Audit
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}