using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text).IsRequired().HasMaxLength(2000);

        builder.HasOne(q => q.User)
            .WithMany(u => u.Quotes)
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Book)
            .WithMany(b => b.Quotes)
            .HasForeignKey(q => q.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.CreatedAt);
    }
}
