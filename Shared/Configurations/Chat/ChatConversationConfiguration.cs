using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Chat
{
    public sealed class ChatConversationConfiguration
    : IEntityTypeConfiguration<ChatConversation>
    {
        public void Configure(EntityTypeBuilder<ChatConversation> builder)
        {
            builder.ToTable("chat_conversations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.InboxId).IsRequired();

            builder.Property(x => x.ContactId).IsRequired();

            builder.Property(x => x.AssignedUserId).IsRequired(false);

            builder.Property(x => x.TeamId).IsRequired(false);

            builder.Property(x => x.Status).IsRequired();

            builder.Property(x => x.Priority).IsRequired();

            builder.Property(x => x.Subject).HasMaxLength(200).IsRequired(false);

            builder.Property(x => x.LastMessageAt).IsRequired();

            builder.Property(x => x.ClosedAt).IsRequired(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            builder.HasOne(x => x.Inbox).WithMany(x => x.Conversations)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Contact).WithMany(x => x.Conversations)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AssignedUser).WithMany()
                .HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Team).WithMany(x => x.Conversations)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Messages).WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Labels).WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Indexes
            // =========================

            builder.HasIndex(x => x.InboxId);

            builder.HasIndex(x => x.ContactId);

            builder.HasIndex(x => x.AssignedUserId);

            builder.HasIndex(x => x.TeamId);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.Priority);

            builder.HasIndex(x => x.LastMessageAt);

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => new
            {
                x.InboxId,
                x.Status,
                x.LastMessageAt
            });

            builder.HasIndex(x => new
            {
                x.AssignedUserId,
                x.Status,
                x.LastMessageAt
            });

            builder.HasIndex(x => new
            {
                x.TeamId,
                x.Status,
                x.LastMessageAt
            });
        }
    }
}
