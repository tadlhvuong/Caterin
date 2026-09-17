using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("chat_messages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.ConversationId).IsRequired();

            builder.Property(x => x.InboxId).IsRequired();

            builder.Property(x => x.MessageType).IsRequired();

            builder.Property(x => x.ContentType).IsRequired();

            builder.Property(x => x.Status).IsRequired();

            builder.Property(x => x.SenderType).IsRequired();

            builder.Property(x => x.ContactId).IsRequired(false);

            builder.Property(x => x.UserId).IsRequired(false);

            builder.Property(x => x.Content).HasMaxLength(150000).IsRequired(false);

            builder.Property(x => x.IsPrivate).IsRequired().HasDefaultValue(false);

            builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            // Conversation
            builder.HasOne(x => x.Conversation).WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

            // Inbox
            builder.HasOne(x => x.Inbox).WithMany(x => x.Messages)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Restrict);

            // Contact
            builder.HasOne(x => x.Contact).WithMany(x => x.Messages)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);

            // User
            builder.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);

            // Attachments
            builder.HasMany(x => x.Attachments).WithOne(x => x.Message)
                .HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);

            // Indexes

            builder.HasIndex(x => x.ConversationId);

            builder.HasIndex(x => x.InboxId);

            builder.HasIndex(x => x.ContactId);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => new
            {
                x.ConversationId,
                x.CreatedAt
            });

            builder.HasIndex(x => new
            {
                x.InboxId,
                x.CreatedAt
            });

            builder.HasIndex(x => new
            {
                x.ConversationId,
                x.IsPrivate,
                x.CreatedAt
            });
        }
    }
}
