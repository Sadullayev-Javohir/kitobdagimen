using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitobdaGimen.Infrastructure.Persistence.Configurations;

public class PhysicalBookReservationConfiguration : IEntityTypeConfiguration<PhysicalBookReservation>
{
    public void Configure(EntityTypeBuilder<PhysicalBookReservation> builder)
    {
        builder.HasKey(r => r.Id);

        // Jismoniy kitob o'chirilsa, uning band qilish yozuvlari ham o'chadi.
        builder.HasOne(r => r.PhysicalBook)
            .WithMany(p => p.Reservations)
            .HasForeignKey(r => r.PhysicalBookId)
            .OnDelete(DeleteBehavior.Cascade);

        // Band qilgan foydalanuvchi: User -> (Owner orqali) allaqachon bitta kaskad yo'li bor,
        // shuning uchun bu yo'nalishni Restrict qilamiz (Postgres ikki kaskad yo'lini rad etadi).
        builder.HasOne(r => r.Reserver)
            .WithMany()
            .HasForeignKey(r => r.ReserverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Muddati o'tgan band qilishlarni topadigan background job uchun.
        builder.HasIndex(r => new { r.IsConfirmed, r.ExpiresAt });
    }
}
