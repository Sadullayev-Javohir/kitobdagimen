using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class StoryLikeConfiguration : IEntityTypeConfiguration<StoryLike>
{
    public void Configure(EntityTypeBuilder<StoryLike> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.HasOne(sl => sl.Story)
            .WithMany(s => s.Likes)
            .HasForeignKey(sl => sl.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.User)
            .WithMany()
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sl => new { sl.StoryId, sl.UserId }).IsUnique();
    }
}
