using Shared.Data.Entities.Identity.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shared.Configurations.Log
{

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();

            // ===== LogBase =====

            builder.Property(x => x.UserId)
                .HasMaxLength(450);

            builder.Property(x => x.UserName)
                .HasMaxLength(100);

            builder.Property(x => x.IpAddress).HasMaxLength(50);

            builder.Property(x => x.UserAgent)
                .HasColumnType("text");

            builder.Property(x => x.TraceId)
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // ===== AuditLog =====

            builder.Property(x => x.ActionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.TableName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.RecordId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.OldValues)
                .HasColumnType("jsonb");

            builder.Property(x => x.NewValues)
                .HasColumnType("jsonb");

            builder.Property(x => x.ChangedColumns)
                .HasColumnType("jsonb");

            // ===== Index =====

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.ActionType);

            builder.HasIndex(x => new
            {
                x.TableName,
                x.RecordId
            });

            builder.HasIndex(x => new
            {
                x.ActionType,
                x.CreatedAt
            });
        }
    }
}
