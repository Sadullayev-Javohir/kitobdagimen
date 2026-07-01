using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class ChallengeWinnerLikeConfiguration : IEntityTypeConfiguration<ChallengeWinnerLike>
{
    public void Configure(EntityTypeBuilder<ChallengeWinnerLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.ChallengeWinner)
            .WithMany(w => w.Likes)
            .HasForeignKey(l => l.ChallengeWinnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ChallengeWinnerId, l.UserId }).IsUnique();
    }
}
