using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Endpoint).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.P256dh).IsRequired().HasMaxLength(300);
        builder.Property(s => s.Auth).IsRequired().HasMaxLength(300);

        // One row per endpoint — re-subscribing upserts instead of duplicating.
        builder.HasIndex(s => s.Endpoint).IsUnique();
        builder.HasIndex(s => s.UserId);

        // Subscriptions cascade away with the user.
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
