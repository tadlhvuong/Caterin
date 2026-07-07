using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.UserId).HasMaxLength(450);

            builder.Property(x => x.ActionType).IsRequired().HasMaxLength(100);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

            builder.Property(x => x.ActionType).HasConversion<string>();

            // ===== Index =====

            builder.HasIndex(x => x.TableName);

            builder.HasIndex(x => new
            {
                x.TableName,
                x.RecordId
            });

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
