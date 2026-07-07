using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Log;

namespace Shared.Configurations.Log
{
    public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
    {
        public void Configure(EntityTypeBuilder<SecurityLog> builder)
        {
            builder.ToTable("SecurityLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).UseIdentityByDefaultColumn();

            // ===== LogBase =====

            builder.Property(x => x.UserId).HasMaxLength(450);

            builder.Property(x => x.UserName).HasMaxLength(100);

            builder.Property(x => x.IpAddress).HasMaxLength(50);

            builder.Property(x => x.UserAgent).HasColumnType("text");

            builder.Property(x => x.TraceId).HasMaxLength(100);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.ActionType).HasConversion<int>().IsRequired();

            builder.Property(x => x.IsSuccess).IsRequired();

            builder.Property(x => x.Message).HasMaxLength(500);

            builder.Property(x => x.TargetUserId).HasMaxLength(200);

            builder.Property(x => x.Resource).HasMaxLength(200);

            builder.Property(x => x.MetadataJson).HasColumnType("jsonb");

            // ===== Index =====

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.ActionType);

            builder.HasIndex(x => x.IsSuccess);

            builder.HasIndex(x => new
            {
                x.ActionType,
                x.IsSuccess
            });

            builder.HasIndex(x => new
            {
                x.UserId,
                x.CreatedAt
            });
        }
    }
}
