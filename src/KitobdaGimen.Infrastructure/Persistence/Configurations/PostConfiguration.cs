using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug).IsRequired().HasMaxLength(16);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.ReviewText).IsRequired().HasMaxLength(5000);
        builder.Property(p => p.ImageUrl).HasMaxLength(2048);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Book)
            .WithMany(b => b.Posts)
            .HasForeignKey(p => p.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CreatedAt);
    }
}
