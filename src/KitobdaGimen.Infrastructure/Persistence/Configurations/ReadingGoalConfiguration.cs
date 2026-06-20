using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class ReadingGoalConfiguration : IEntityTypeConfiguration<ReadingGoal>
{
    public void Configure(EntityTypeBuilder<ReadingGoal> builder)
    {
        builder.HasKey(rg => rg.Id);

        builder.HasOne(rg => rg.User)
            .WithMany(u => u.ReadingGoals)
            .HasForeignKey(rg => rg.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rg => rg.Book)
            .WithMany(b => b.ReadingGoals)
            .HasForeignKey(rg => rg.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rg => new { rg.UserId, rg.IsActive });
    }
}
