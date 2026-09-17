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
    public sealed class ChatTeamMemberConfiguration
    : IEntityTypeConfiguration<ChatTeamMember>
    {
        public void Configure(EntityTypeBuilder<ChatTeamMember> builder)
        {
            builder.ToTable("chat_team_members");

            builder.HasKey(x => new
            {
                x.TeamId,
                x.UserId
            });

            builder.Property(x => x.TeamId).IsRequired();

            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(x => x.Team).WithMany(x => x.Members)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Index
            // =========================

            builder.HasIndex(x => x.UserId);
        }
    }
}
