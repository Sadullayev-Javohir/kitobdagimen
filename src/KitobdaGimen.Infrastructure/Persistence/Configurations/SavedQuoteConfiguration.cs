using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class SavedQuoteConfiguration : IEntityTypeConfiguration<SavedQuote>
{
    public void Configure(EntityTypeBuilder<SavedQuote> builder)
    {
        builder.HasKey(sq => sq.Id);

        builder.HasOne(sq => sq.Quote)
            .WithMany(q => q.SavedBy)
            .HasForeignKey(sq => sq.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sq => sq.User)
            .WithMany(u => u.SavedQuotes)
            .HasForeignKey(sq => sq.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sq => new { sq.QuoteId, sq.UserId }).IsUnique();
    }
}
