using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations
{
    public sealed class CurrencyConfiguration
    : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("Currencies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(10);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Symbol).IsRequired().HasMaxLength(10);

            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        }
    }
}
