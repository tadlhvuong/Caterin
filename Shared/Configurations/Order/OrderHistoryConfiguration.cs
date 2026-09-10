using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

namespace Shared.Configurations.Order
{
    public class OrderHistoryConfiguration : IEntityTypeConfiguration<OrderHistory>
    {
        public void Configure(EntityTypeBuilder<OrderHistory> builder)
        {
            builder.ToTable("OrderHistories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.OrderId).IsRequired();

            builder.Property(x => x.Status).IsRequired().HasConversion<int>();

            builder.Property(x => x.Note).HasMaxLength(1000).IsRequired(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(x => x.Order).WithMany(x => x.Histories)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.OrderId);

            builder.HasIndex(x => new
            {
                x.OrderId,
                x.CreatedAt
            });
        }
    }
}