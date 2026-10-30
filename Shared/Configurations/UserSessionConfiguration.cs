using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Configurations
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UserSessions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DeviceName)
                .HasMaxLength(256);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(1000);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(45);

            builder.Property(x => x.RevokedReason)
                .HasMaxLength(256);

            builder.Property(x => x.RevokedById)
                .HasMaxLength(450);

            builder.Property(x => x.CreatedAt)
                .IsRequired().HasColumnType("timestamp with time zone");

            builder.Property(x => x.LastActivityAt)
                .IsRequired().HasColumnType("timestamp with time zone");

            builder.Property(x => x.ExpiresAt)
                .IsRequired().HasColumnType("timestamp with time zone");

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new { x.UserId, x.RevokedAt });

            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.RefreshTokens).WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);


        }
    }
}