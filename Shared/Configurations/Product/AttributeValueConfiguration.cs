using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

namespace Shared.Configurations.Product
{
    public class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
    {
        public void Configure(EntityTypeBuilder<AttributeValue> builder)
        {
            builder.ToTable("AttributeValues");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Value).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.Attribute).WithMany(x => x.Values)
                .HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.HasIndex(x => new
            {
                x.AttributeId,
                x.Value
            }).IsUnique();
        }
    }
}