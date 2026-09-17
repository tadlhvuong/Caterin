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
    public sealed class ChatTeamConfiguration
    : IEntityTypeConfiguration<ChatTeam>
    {
        public void Configure(EntityTypeBuilder<ChatTeam> builder)
        {
            builder.ToTable("chat_teams");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);

            builder.Property(x => x.AvatarUrl).HasMaxLength(500).IsRequired(false);

            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            builder.HasMany(x => x.Members).WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Conversations).WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);

            // =========================
            // Index
            // =========================

            builder.HasIndex(x => x.Name);
        }
    }
}
