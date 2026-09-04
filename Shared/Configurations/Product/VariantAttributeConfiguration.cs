using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

public class VariantAttributeConfiguration : IEntityTypeConfiguration<VariantAttribute>
{
    public void Configure(EntityTypeBuilder<VariantAttribute> builder)
    {
        builder.ToTable("VariantAttributes");

        builder.HasKey(x => new
        {
            x.ProductVariantId,
            x.AttributeValueId
        });

        // =========================
        // ProductVariant relationship
        // =========================

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.VariantAttributes)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // AttributeValue relationship
        // =========================

        builder.HasOne(x => x.AttributeValue)
            .WithMany(x => x.VariantAttributes)
            .HasForeignKey(x => x.AttributeValueId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Index
        // =========================

        builder.HasIndex(x => x.AttributeValueId);
    }
}