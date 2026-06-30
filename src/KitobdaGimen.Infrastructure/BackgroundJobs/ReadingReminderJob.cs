using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Har kuni (O'zbekiston vaqti bilan 20:00 da) ishga tushadigan eslatma jobi.
/// Faol o'qish maqsadi (kitob) bori, lekin BUGUN bironta bet o'qimagan har bir
/// foydalanuvchiga "bugun kitob o'qing" bildirishnomasini yuboradi. Bildirishnoma
/// boyo'g'li (🦉) tomonidan yetkaziladi — `NotificationType` = "reading_reminder",
/// frontend uni `kitob:notification` event orqali boyo'g'liga uzatadi.
/// </summary>
public class ReadingReminderJob
{
    /// <summary>Bildirishnoma turi — frontend boyo'g'lisi shu turni tinglaydi.</summary>
    public const string NotificationType = "reading_reminder";

    /// <summary>Hangfire recurring job identifikatori.</summary>
    public const string RecurringJobId = "daily-reading-reminder";

    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ReadingReminderJob> _logger;

    public ReadingReminderJob(
        IAppDbContext db, INotificationService notifications, ILogger<ReadingReminderJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    /// <summary>
    /// Faol maqsadi bor, ammo bugun hech narsa o'qimagan foydalanuvchilarni topib,
    /// har biriga bitta eslatma yuboradi. "Bugun" — O'zbekiston (UTC+5) sanasiga ko'ra.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Hangfire serveri UTC'da ishlaydi; O'zbekiston UTC+5. "Bugungi" sana shu mintaqada
        // (o'qish progressi ham shu sana bilan saqlanadi — moslik kafolatlanadi).
        var today = KitobdaGimen.Application.Common.UzTime.Today;

        // Faol o'qish maqsadi (tugatilmagan kitob) bor foydalanuvchilar.
        var usersWithActiveGoals = await _db.ReadingGoals
            .Where(g => g.IsActive)
            .Select(g => g.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (usersWithActiveGoals.Count == 0)
        {
            _logger.LogInformation("Kunlik eslatma: faol maqsadli foydalanuvchi yo'q.");
            return;
        }

        // Bugun kamida 1 bet o'qiganlar (eslatma kerak emas).
        var readToday = await _db.ReadingProgress
            .Where(p => p.Date == today && p.PagesReadToday > 0)
            .Select(p => p.ReadingGoal.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var recipients = usersWithActiveGoals.Except(readToday).ToList();
        if (recipients.Count == 0)
        {
            _logger.LogInformation("Kunlik eslatma: hamma bugun o'qibdi, eslatma yuborilmadi.");
            return;
        }

        var notification = new NotificationDto
        {
            Type = NotificationType,
            ActorName = "Boyo'g'li",
            Message = "Bugun hali kitob o'qimadingiz. Bir oz o'qishga vaqt toping! 🦉📖",
            Url = "/reading-books",
            ActorAvatarUrl = "/img/icons/icon-192.png"
        };

        await _notifications.NotifyManyAsync(recipients, notification, cancellationToken);

        _logger.LogInformation(
            "Kunlik o'qish eslatmasi {Count} foydalanuvchiga yuborildi.", recipients.Count);
    }
}
