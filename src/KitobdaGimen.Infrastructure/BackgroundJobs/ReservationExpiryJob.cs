using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Muddati o'tgan (24 soatdan oshgan, lekin egasi hali topshirmagan) band qilishlarni
/// avtomatik bekor qiladi: band qilish yozuvini o'chirib, kitob statusini "Mavjud"ga
/// qaytaradi. Har 15 daqiqada ishga tushadi.
/// </summary>
public class ReservationExpiryJob
{
    /// <summary>Hangfire recurring job identifikatori.</summary>
    public const string RecurringJobId = "physical-book-reservation-expiry";

    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ReservationExpiryJob> _logger;

    public ReservationExpiryJob(
        IAppDbContext db, INotificationService notifications, ILogger<ReservationExpiryJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Tasdiqlanmagan va muddati o'tgan band qilishlar. Kitob hali "Band qilindi"da
        // bo'lgani muhim — egasi topshirgan bo'lsa (O'qiyapti) tegmaymiz.
        // Faqat xabardor qilish uchun Id/ReserverId ni olishimiz kerak; o'zgarishlar
        // quyida atomik, server tomonidagi shartli UPDATE/DELETE bilan bajariladi.
        var candidates = await _db.PhysicalBookReservations
            .Where(r => !r.IsConfirmed
                        && r.ExpiresAt <= now
                        && r.PhysicalBook.Status == PhysicalBookStatus.BandQilindi)
            .Select(r => new { r.Id, r.ReserverId })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return;
        }

        var expiredCount = 0;
        foreach (var candidate in candidates)
        {
            // Kitobni "Mavjud"ga faqat shartlar hali ham to'g'ri bo'lsa o'tkazamiz —
            // ya'ni u hali "Band qilindi"da va band qilish hali tasdiqlanmagan. SHART
            // serverda, yangilash vaqtida baholanishadi, shuning uchun egasi aynan shu
            // orada topshirishni tasdiqlasa (IsConfirmed=true, Status=O'qiyapti) bu UPDATE
            // 0 ta qatorni o'zgartiradi va kitobga tegmaymiz (eski tracking entiteti
            // bilan ustiga yozish muammosi yo'q).
            var updated = await _db.PhysicalBooks
                .Where(pb => pb.Status == PhysicalBookStatus.BandQilindi
                             && pb.Reservations.Any(r => r.Id == candidate.Id
                                                          && !r.IsConfirmed
                                                          && r.ExpiresAt <= now))
                .ExecuteUpdateAsync(pb => pb.SetProperty(p => p.Status, PhysicalBookStatus.Mavjud),
                    cancellationToken);

            if (updated == 0)
            {
                // Parallel tasdiqlash birinchi bo'lib sodir bo'ldi — bu band qilishni o'tkazib yuboramiz.
                continue;
            }

            // Kitob haqiqatan ham bizga qaytgan: band qilish yozuvini o'chiramiz va foydalanuvchini xabardor qilamiz.
            await _db.PhysicalBookReservations
                .Where(r => r.Id == candidate.Id && !r.IsConfirmed)
                .ExecuteDeleteAsync(cancellationToken);

            await _notifications.NotifyAsync(candidate.ReserverId, new NotificationDto
            {
                Type = "physical_book_expired",
                ActorName = "Kutubxona",
                Message = "Band qilingan kitobingiz muddati (24 soat) tugadi va yana boshqalarga mavjud bo'ldi.",
                Url = "/almashish"
            }, cancellationToken);

            expiredCount++;
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "Muddati o'tgan {Count} ta kitob band qilishi bekor qilindi.", expiredCount);
        }
    }
}
