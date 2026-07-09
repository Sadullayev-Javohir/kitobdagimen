using KitobdaGimen.Domain.Common;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Foydalanuvchi o'zida mavjud bo'lgan jismoniy kitob. Boshqa foydalanuvchilar
/// uni band qilib, keyin olib o'qishi mumkin.
/// </summary>
public class PhysicalBook : BaseEntity
{
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    /// <summary>Katalogdagi kitob (ixtiyoriy — qo'lda kiritilgan nom ham bo'lishi mumkin).</summary>
    public int? BookId { get; set; }
    public Book? Book { get; set; }

    /// <summary>Agar katalogda yo'q bo'lsa qo'lda kiritilgan nom.</summary>
    public string? ManualTitle { get; set; }
    public string? ManualAuthor { get; set; }

    public PhysicalBookStatus Status { get; set; } = PhysicalBookStatus.Mavjud;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<PhysicalBookReservation> Reservations { get; set; } = new List<PhysicalBookReservation>();
}
