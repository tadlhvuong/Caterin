using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public class ChatInboxConfiguration : IEntityTypeConfiguration<ChatInbox>
    {
        public void Configure(EntityTypeBuilder<ChatInbox> builder)
        {
            builder.ToTable("chat_inboxes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.ChannelType).IsRequired();

            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(x => x.SettingsJson).HasColumnType("jsonb");

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            builder.HasMany(x => x.Conversations).WithOne(x => x.Inbox)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(x => x.Messages).WithOne(x => x.Inbox)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Contacts).WithOne(x => x.Inbox)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(x => x.ChannelType);

            builder.HasIndex(x => x.IsActive);

            builder.HasIndex(x => new
            {
                x.IsActive,
                x.ChannelType
            });
        }
    }
}
