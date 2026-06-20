using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class StoryViewConfiguration : IEntityTypeConfiguration<StoryView>
{
    public void Configure(EntityTypeBuilder<StoryView> builder)
    {
        builder.HasKey(sv => sv.Id);

        builder.HasOne(sv => sv.Story)
            .WithMany(s => s.Views)
            .HasForeignKey(sv => sv.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sv => sv.User)
            .WithMany()
            .HasForeignKey(sv => sv.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One distinct view record per user per story.
        builder.HasIndex(sv => new { sv.StoryId, sv.UserId }).IsUnique();
    }
}
