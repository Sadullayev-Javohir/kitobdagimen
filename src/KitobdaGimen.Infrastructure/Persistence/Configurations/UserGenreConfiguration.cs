using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class UserGenreConfiguration : IEntityTypeConfiguration<UserGenre>
{
    public void Configure(EntityTypeBuilder<UserGenre> builder)
    {
        builder.HasKey(ug => new { ug.UserId, ug.GenreId });

        builder.HasOne(ug => ug.User)
            .WithMany(u => u.UserGenres)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ug => ug.Genre)
            .WithMany(g => g.UserGenres)
            .HasForeignKey(ug => ug.GenreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
