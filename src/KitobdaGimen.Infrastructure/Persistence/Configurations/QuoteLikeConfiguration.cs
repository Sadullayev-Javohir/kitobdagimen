using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class QuoteLikeConfiguration : IEntityTypeConfiguration<QuoteLike>
{
    public void Configure(EntityTypeBuilder<QuoteLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Quote)
            .WithMany(q => q.Likes)
            .HasForeignKey(l => l.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.QuoteId, l.UserId }).IsUnique();
    }
}
