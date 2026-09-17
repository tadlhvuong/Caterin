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
    public sealed class ChatLabelConfiguration
    : IEntityTypeConfiguration<ChatLabel>
    {
        public void Configure(EntityTypeBuilder<ChatLabel> builder)
        {
            builder.ToTable("chat_labels");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Color).HasMaxLength(50).IsRequired(false);

            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            builder.HasMany(x => x.Conversations).WithOne(x => x.Label)
                .HasForeignKey(x => x.LabelId).OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Indexes
            // =========================

            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
