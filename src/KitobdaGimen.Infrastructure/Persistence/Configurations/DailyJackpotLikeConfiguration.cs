using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class DailyJackpotLikeConfiguration : IEntityTypeConfiguration<DailyJackpotLike>
{
    public void Configure(EntityTypeBuilder<DailyJackpotLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.DailyJackpotId, l.UserId })
            .IsUnique();

        builder.HasOne(l => l.DailyJackpot)
            .WithMany(j => j.Likes)
            .HasForeignKey(l => l.DailyJackpotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
