using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class ChallengeWinnerConfiguration : IEntityTypeConfiguration<ChallengeWinner>
{
    public void Configure(EntityTypeBuilder<ChallengeWinner> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.GiftBookTitle).HasMaxLength(300);
        builder.Property(w => w.GiftBookAuthor).HasMaxLength(200);
        builder.Property(w => w.GiftBookCoverUrl).HasMaxLength(2048);

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bir davrda (yil+oy) har o'rin va har foydalanuvchi faqat bir marta.
        builder.HasIndex(w => new { w.Year, w.Month, w.Rank }).IsUnique();
        builder.HasIndex(w => new { w.Year, w.Month, w.UserId }).IsUnique();
        builder.HasIndex(w => w.UserId);
    }
}
