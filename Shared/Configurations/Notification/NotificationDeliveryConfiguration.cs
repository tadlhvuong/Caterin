using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Notification
{
    public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
    {
        public void Configure(
            EntityTypeBuilder<NotificationDelivery> builder)
        {
            builder.ToTable("NotificationDeliveries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NotificationId)
                .IsRequired();

            builder.Property(x => x.Channel)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.SendAt).HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasOne(x => x.Notification)
                .WithMany(x => x.Deliveries)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.NotificationId);
        }
    }
}
