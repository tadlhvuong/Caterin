using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity;

public class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ReplacedByTokenHash)
            .IsRequired(false)
            .HasMaxLength(64);

        builder.Property(x => x.JwtId)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(500);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(50);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.IsRevoked)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiredAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.ExpiredAt);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IsRevoked
        });
    }
}