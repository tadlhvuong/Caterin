using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Configurations
{
    public class EmailActionConfiguration : IEntityTypeConfiguration<EmailAction>
    {
        public void Configure(EntityTypeBuilder<EmailAction> builder)
        {
            builder.ToTable("EmailActions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.KeyHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(x => x.KeyHash)
                .IsUnique();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Token)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiredAt)
                .IsRequired();

            builder.Property(x => x.RevokedReason)
                .HasMaxLength(500);

            builder.HasOne(x => x.User)
                .WithMany(x => x.EmailActions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.UserId,
                x.Type
            });

            builder.HasIndex(x => x.ExpiredAt);

            builder.HasIndex(x => x.UsedAt);

            builder.HasIndex(x => x.RevokedAt);
        }
    }
}
