using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class PhysicalBookConfiguration : IEntityTypeConfiguration<PhysicalBook>
{
    public void Configure(EntityTypeBuilder<PhysicalBook> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ManualTitle).HasMaxLength(150);
        builder.Property(p => p.ManualAuthor).HasMaxLength(150);
        builder.Property(p => p.Status).HasConversion<int>();

        // Egasi o'chirilsa, uning jismoniy kitoblari ham o'chadi.
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Katalog kitobi ixtiyoriy; katalog yozuvi o'chsa jismoniy kitobni ushlab qolamiz.
        builder.HasOne(p => p.Book)
            .WithMany()
            .HasForeignKey(p => p.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Kutubxona va "mening kitoblarim" so'rovlari status/egaga tayanadi.
        builder.HasIndex(p => new { p.OwnerId, p.Status });
        builder.HasIndex(p => p.Status);
    }
}
