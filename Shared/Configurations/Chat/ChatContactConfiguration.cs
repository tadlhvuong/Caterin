using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public sealed class ChatContactConfiguration : IEntityTypeConfiguration<ChatContact>
    {
        public void Configure(EntityTypeBuilder<ChatContact> builder)
        {
            builder.ToTable("chat_contacts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.UserId).IsRequired(false);

            builder.Property(x => x.Name).HasMaxLength(150).IsRequired(false);

            builder.Property(x => x.Email).HasMaxLength(255).IsRequired(false);

            builder.Property(x => x.Phone).HasMaxLength(30).IsRequired(false);

            builder.Property(x => x.AvatarUrl).HasMaxLength(500).IsRequired(false);

            builder.Property(x => x.CustomAttributesJson).HasColumnType("jsonb").IsRequired(false);

            builder.Property(x => x.IsMerged).HasDefaultValue(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            builder.Property(x => x.MergedAt).HasColumnType("timestamp with time zone");

            builder.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Inboxes).WithOne(x => x.Contact)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Conversations).WithOne(x => x.Contact)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MergedIntoContact).WithMany(x => x.MergedContacts)
    .HasForeignKey(x => x.MergedIntoContactId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Messages).WithOne(x => x.Contact)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);

            
            // Indexes

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.Email);

            builder.HasIndex(x => x.Phone);

            builder.HasIndex(x => x.MergedIntoContactId);
        }
    }
}
