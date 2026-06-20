using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.GoogleId).IsRequired().HasMaxLength(128);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Username).HasMaxLength(32);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Bio).HasMaxLength(500);

        builder.HasIndex(u => u.GoogleId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
