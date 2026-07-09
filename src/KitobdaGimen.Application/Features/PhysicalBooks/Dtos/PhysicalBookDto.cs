using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Dtos;

/// <summary>
/// Foydalanuvchi qo'shgan jismoniy kitob — kutubxona ro'yxatida va "mening kitoblarim"da
/// ko'rsatiladi. Nom/muallif katalogdagi kitobdan yoki qo'lda kiritilgan qiymatdan olinadi.
/// </summary>
public record PhysicalBookDto
{
    public int Id { get; init; }

    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public string? CoverUrl { get; init; }

    public PhysicalBookStatus Status { get; init; }
    /// <summary>Statusning o'zbekcha ko'rinishi (badge uchun): "Mavjud", "Band qilindi", "O'qiyapti".</summary>
    public string StatusText { get; init; } = null!;

    public int OwnerId { get; init; }
    public string OwnerName { get; init; } = null!;
    public string? OwnerUsername { get; init; }
    public string? OwnerAvatarUrl { get; init; }

    /// <summary>So'rov yuborgan foydalanuvchi shu kitobning egasimi.</summary>
    public bool IsMine { get; init; }

    // ----- Joriy band qilish (agar status Mavjud bo'lmasa) -----

    public int? ReserverId { get; init; }
    public string? ReserverName { get; init; }
    public string? ReserverUsername { get; init; }
    public string? ReserverAvatarUrl { get; init; }

    /// <summary>Band qilish muddati tugash vaqti (UTC). Faqat "Band qilindi" holatida to'ladi.</summary>
    public DateTime? ReservationExpiresAt { get; init; }

    /// <summary>So'rov yuborgan foydalanuvchi shu kitobni band qilgan/olgan odammi.</summary>
    public bool ReservedByMe { get; init; }

    public DateTime CreatedAt { get; init; }
}
