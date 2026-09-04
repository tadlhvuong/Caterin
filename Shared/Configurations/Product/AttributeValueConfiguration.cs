using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

public class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
{
    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.ToTable("AttributeValues");

        builder.HasKey(x => x.Id);

        // =========================
        // Information
        // =========================

        builder.Property(x => x.Value)
            .IsRequired()
            .HasMaxLength(100);

        // =========================
        // Attribute relationship
        // =========================

        builder.HasOne(x => x.Attribute)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Audit
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // =========================
        // Index
        // =========================

        // Không cho phép cùng một Attribute
        // có hai Value giống nhau
        builder.HasIndex(x => new
        {
            x.AttributeId,
            x.Value
        })
        .IsUnique();
    }
}