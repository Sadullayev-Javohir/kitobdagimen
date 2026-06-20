using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(g => g.Name).IsUnique();

        // Canonical onboarding genres. Fixed ids so sample/seed data and the
        // onboarding icon map (Views/Onboarding/Index.cshtml) stay deterministic.
        // Order matches the design reference (02-janr-tanlash).
        builder.HasData(
            new Genre { Id = 1, Name = "Roman" },
            new Genre { Id = 2, Name = "Ilmiy" },
            new Genre { Id = 3, Name = "Detektiv" },
            new Genre { Id = 4, Name = "Biografiya" },
            new Genre { Id = 5, Name = "Falsafa" },
            new Genre { Id = 6, Name = "Biznes" },
            new Genre { Id = 7, Name = "She'riyat" },
            new Genre { Id = 8, Name = "Tarix" },
            new Genre { Id = 9, Name = "Fantastika" },
            new Genre { Id = 10, Name = "Psixologiya" });
    }
}
