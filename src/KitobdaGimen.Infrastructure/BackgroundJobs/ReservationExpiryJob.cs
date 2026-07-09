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
        var expired = await _db.PhysicalBookReservations
            .Include(r => r.PhysicalBook)
            .Where(r => !r.IsConfirmed
                        && r.ExpiresAt <= now
                        && r.PhysicalBook.Status == PhysicalBookStatus.BandQilindi)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var reservation in expired)
        {
            reservation.PhysicalBook.Status = PhysicalBookStatus.Mavjud;
            _db.PhysicalBookReservations.Remove(reservation);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Band qilgan foydalanuvchilarni xabardor qilamiz — muddat o'tdi, kitob yana mavjud.
        foreach (var reservation in expired)
        {
            await _notifications.NotifyAsync(reservation.ReserverId, new NotificationDto
            {
                Type = "physical_book_expired",
                ActorName = "Kutubxona",
                Message = "Band qilingan kitobingiz muddati (24 soat) tugadi va yana boshqalarga mavjud bo'ldi.",
                Url = "/almashish"
            }, cancellationToken);
        }

        _logger.LogInformation(
            "Muddati o'tgan {Count} ta kitob band qilishi bekor qilindi.", expired.Count);
    }
}
