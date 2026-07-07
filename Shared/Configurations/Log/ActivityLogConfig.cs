using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Log;

namespace Shared.Configurations.Log
{
    public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder.ToTable("ActivityLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();

            // ===== LogBase =====

            builder.Property(x => x.UserId)
                .HasMaxLength(450);

            builder.Property(x => x.UserName)
                .HasMaxLength(100);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(50);

            builder.Property(x => x.UserAgent)
                .HasColumnType("text");

            builder.Property(x => x.TraceId)
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // ===== AuditLog =====

            builder.Property(x => x.ActivityType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.EntityType)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.MetadataJson)
                .HasColumnType("jsonb");

            // ===== Index =====

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => new
            {
                x.EntityType,
                x.EntityId
            });

            builder.HasIndex(x => new { x.ActivityType, x.CreatedAt });
        }
    }
}
