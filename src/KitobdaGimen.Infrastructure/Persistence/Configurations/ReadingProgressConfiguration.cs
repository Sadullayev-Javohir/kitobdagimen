using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class ReadingProgressConfiguration : IEntityTypeConfiguration<ReadingProgress>
{
    public void Configure(EntityTypeBuilder<ReadingProgress> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.HasOne(rp => rp.ReadingGoal)
            .WithMany(rg => rg.Progress)
            .HasForeignKey(rp => rp.ReadingGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // One progress row per goal per day.
        builder.HasIndex(rp => new { rp.ReadingGoalId, rp.Date }).IsUnique();
    }
}
