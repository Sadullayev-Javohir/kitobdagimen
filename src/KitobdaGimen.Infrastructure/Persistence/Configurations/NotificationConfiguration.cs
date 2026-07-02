using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).IsRequired().HasMaxLength(40);
        builder.Property(n => n.ActorName).HasMaxLength(120);
        builder.Property(n => n.Title).HasMaxLength(150);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(300);

        // The recipient's notifications cascade away with the user (single FK path, no ambiguity).
        builder.HasOne(n => n.Recipient)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        // ActorId is a plain id (no FK) so the actor can delete their account without orphaning rows.

        // Fast lookup of a recipient's unread notifications, newest first.
        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
    }
}
