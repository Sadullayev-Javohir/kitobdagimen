using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class QuoteViewConfiguration : IEntityTypeConfiguration<QuoteView>
{
    public void Configure(EntityTypeBuilder<QuoteView> builder)
    {
        builder.HasKey(qv => qv.Id);

        builder.HasOne(qv => qv.Quote)
            .WithMany(q => q.Views)
            .HasForeignKey(qv => qv.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qv => qv.User)
            .WithMany()
            .HasForeignKey(qv => qv.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One distinct view record per user per quote.
        builder.HasIndex(qv => new { qv.QuoteId, qv.UserId }).IsUnique();
    }
}
