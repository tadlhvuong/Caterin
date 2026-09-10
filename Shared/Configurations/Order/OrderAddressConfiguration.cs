using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

namespace Shared.Configurations.Order
{
    public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
    {
        public void Configure(EntityTypeBuilder<OrderAddress> builder)
        {
            builder.ToTable("OrderAddresses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.OrderId).IsRequired();

            builder.Property(x => x.ReceiverName).IsRequired().HasMaxLength(200);

            builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);

            builder.Property(x => x.Province).IsRequired().HasMaxLength(100);

            builder.Property(x => x.District).HasMaxLength(100).IsRequired(false);

            builder.Property(x => x.Ward).IsRequired().HasMaxLength(100);

            builder.Property(x => x.AddressLine).IsRequired().HasMaxLength(500);

            builder.HasOne(x => x.Order).WithOne(x => x.Address)
                .HasForeignKey<OrderAddress>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.OrderId).IsUnique();
        }
    }
}