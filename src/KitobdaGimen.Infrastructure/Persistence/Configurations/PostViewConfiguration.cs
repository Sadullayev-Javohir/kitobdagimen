using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class PostViewConfiguration : IEntityTypeConfiguration<PostView>
{
    public void Configure(EntityTypeBuilder<PostView> builder)
    {
        builder.HasKey(pv => pv.Id);

        builder.HasOne(pv => pv.Post)
            .WithMany(p => p.Views)
            .HasForeignKey(pv => pv.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One distinct view record per user per post.
        builder.HasIndex(pv => new { pv.PostId, pv.UserId }).IsUnique();
    }
}
