using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class MorningQuoteConfiguration : IEntityTypeConfiguration<MorningQuote>
{
    public void Configure(EntityTypeBuilder<MorningQuote> builder)
    {
        builder.ToTable("MorningQuotes");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.Author).IsRequired().HasMaxLength(200);
        builder.Property(q => q.Source).HasMaxLength(300);
        builder.Property(q => q.OrderIndex).HasDefaultValue(0);
        builder.Property(q => q.CreatedAt).IsRequired();

        builder.HasIndex(q => q.OrderIndex);
    }
}

