using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<int>();

        builder.HasOne(c => c.Requester)
            .WithMany(u => u.SentConnections)
            .HasForeignKey(c => c.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Addressee)
            .WithMany(u => u.ReceivedConnections)
            .HasForeignKey(c => c.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        // One connection row per ordered (requester, addressee) pair. The reverse pair is
        // checked in the handler (auto-accept when both invite each other).
        builder.HasIndex(c => new { c.RequesterId, c.AddresseeId }).IsUnique();
    }
}
