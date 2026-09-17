using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public class ChatGuestSessionConfiguration
    : IEntityTypeConfiguration<ChatGuestSession>
    {
        public void Configure(EntityTypeBuilder<ChatGuestSession> builder)
        {
            builder.ToTable("chat_guest_sessions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.ContactId).IsRequired();

            builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.LastSeenAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(x => x.Contact)
                .WithMany()
                .HasForeignKey(x => x.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.ContactId,
                x.IsRevoked,
                x.ExpiresAt
            });
        }
    }
}
