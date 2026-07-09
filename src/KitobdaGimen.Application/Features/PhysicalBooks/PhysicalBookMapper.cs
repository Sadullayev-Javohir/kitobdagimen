using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Features.PhysicalBooks;

/// <summary>
/// <see cref="PhysicalBook"/>ni <see cref="PhysicalBookDto"/>ga o'giradi. Nom/muallif
/// katalogdagi kitobdan yoki qo'lda kiritilgan qiymatdan olinadi; joriy band qilish
/// (eng so'nggi yozuv) status Mavjud bo'lmaganda ko'rsatiladi. Foydalanuvchiga bog'liq
/// (IsMine/ReservedByMe) bo'lgani uchun xotirada, EF proyeksiyasidan keyin bajariladi.
/// </summary>
public static class PhysicalBookMapper
{
    public static PhysicalBookDto ToDto(PhysicalBook b, int? currentUserId)
    {
        // Status Mavjud bo'lmaganda joriy band qilish = eng so'nggi yozuv.
        var active = b.Status == PhysicalBookStatus.Mavjud
            ? null
            : b.Reservations.OrderByDescending(r => r.ReservedAt).FirstOrDefault();

        return new PhysicalBookDto
        {
            Id = b.Id,
            Title = b.Book?.Title ?? b.ManualTitle ?? "Noma'lum kitob",
            Author = b.Book?.Author ?? b.ManualAuthor ?? "Noma'lum muallif",
            CoverUrl = b.Book?.CoverUrl,

            Status = b.Status,
            StatusText = StatusText(b.Status),

            OwnerId = b.OwnerId,
            OwnerName = b.Owner.FullName,
            OwnerUsername = b.Owner.Username,
            OwnerAvatarUrl = b.Owner.AvatarUrl,
            IsMine = currentUserId is not null && b.OwnerId == currentUserId,

            ReserverId = active?.ReserverId,
            ReserverName = active?.Reserver.FullName,
            ReserverUsername = active?.Reserver.Username,
            ReserverAvatarUrl = active?.Reserver.AvatarUrl,
            ReservationExpiresAt = b.Status == PhysicalBookStatus.BandQilindi ? active?.ExpiresAt : null,
            ReservedByMe = currentUserId is not null && active?.ReserverId == currentUserId,

            CreatedAt = b.CreatedAt
        };
    }

    private static string StatusText(PhysicalBookStatus status) => status switch
    {
        PhysicalBookStatus.Mavjud => "Mavjud",
        PhysicalBookStatus.BandQilindi => "Band qilindi",
        PhysicalBookStatus.OqiyApti => "O'qiyapti",
        _ => status.ToString()
    };
}
