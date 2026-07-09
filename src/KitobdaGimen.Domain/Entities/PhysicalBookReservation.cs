using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Kitobni band qilish yozuvi. Band qilish 24 soat davom etadi;
/// agar foydalanuvchi kitobni olmasa, status avtomatik "Mavjud"ga qaytadi.
/// </summary>
public class PhysicalBookReservation : BaseEntity
{
    public int PhysicalBookId { get; set; }
    public PhysicalBook PhysicalBook { get; set; } = null!;

    public int ReserverId { get; set; }
    public User Reserver { get; set; } = null!;

    public DateTime ReservedAt { get; set; }

    /// <summary>Band qilish muddati tugash vaqti (ReservedAt + 24 soat).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Foydalanuvchi kitobni olganda true bo'ladi.</summary>
    public bool IsConfirmed { get; set; }
}
