
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;
using OrderEntity = Shared.Data.Entities.Order.Order;

namespace Shared.Configurations.Order
{
    public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
    {
        public void Configure(EntityTypeBuilder<OrderEntity> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(50);

            builder.HasIndex(x => x.OrderCode).IsUnique();

            builder.Property(x => x.Status).IsRequired().HasConversion<int>();

            builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.ShippingAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(x => x.Note).HasMaxLength(1000).IsRequired(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Address).WithOne(x => x.Order)
                .HasForeignKey<OrderAddress>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Items).WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Histories).WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}