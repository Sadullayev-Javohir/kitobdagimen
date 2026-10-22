using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Har kuni ertalab soat 05:30 da (O'zbekiston vaqti, UTC 00:30) ishga tushadigan fon vazifasi.
/// Bazadagi navbatdagi (eng birinchi turgan) erta tonggi motivatsion iqtibosni olib,
/// Telegram guruhiga, bot foydalanuvchilariga va saytga e'lon qiladi.
/// E'lon qilingandan so'ng, ushbu iqtibos avtomatik ravishda PostgreSQL ma'lumotlar bazasidan
/// o'chirib tashlanadi (FIFO navbat mexanizmi).
/// </summary>
public class MorningQuoteJob
{
    public const string RecurringJobId = "daily-morning-quote";
    public const string NotificationType = "morning_quote";

    private readonly IAppDbContext _db;
    private readonly ITelegramBotService _telegramBot;
    private readonly INotificationService _notifications;
    private readonly ILogger<MorningQuoteJob> _logger;

    public MorningQuoteJob(
        IAppDbContext db,
        ITelegramBotService telegramBot,
        INotificationService notifications,
        ILogger<MorningQuoteJob> logger)
    {
        _db = db;
        _telegramBot = telegramBot;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MorningQuoteJob ishga tushdi: Soat 05:30 erta tonggi iqtibos tekshirilmoqda.");

        // PostgreSQL dan eng birinchi qo'yilgan (FIFO) iqtibosni olamiz
        var quote = await _db.MorningQuotes
            .OrderBy(q => q.OrderIndex)
            .ThenBy(q => q.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (quote is null)
        {
            _logger.LogWarning("MorningQuoteJob: Bazada tarqatish uchun erta tonggi iqtiboslar qolmagan!");
            return;
        }

        _logger.LogInformation(
            "05:30 Iqtibosi tanlandi (Id={Id}, OrderIndex={Order}): Muallif={Author}, Kitob={Source}",
            quote.Id, quote.OrderIndex, quote.Author, quote.Source);

        try
        {
            // 1. Telegram guruhiga va bot orqali foydalanuvchilarga yuborish
            await _telegramBot.BroadcastMorningQuoteAsync(
                quote.Text, quote.Author, quote.Source, cancellationToken);

            // 2. Sayt foydalanuvchilariga veb-bildirishnoma va push yuborish
            var activeUserIds = await _db.Users
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (activeUserIds.Count > 0)
            {
                var shortText = quote.Text.Length > 90
                    ? quote.Text[..87] + "..."
                    : quote.Text;

                var webNotification = new NotificationDto
                {
                    Type = NotificationType,
                    ActorName = "KitobdagiMen",
                    Message = $"🌅 {quote.Author}: «{shortText}» Xayrli tong, kitob mutolaasi vaqti bo'ldi!",
                    Url = "/reading-books",
                    ActorAvatarUrl = "/img/icons/icon-192.png"
                };

                await _notifications.NotifyManyAsync(activeUserIds, webNotification, cancellationToken);
            }

            // 3. Talab: "birinchi qo'yilgan iqtibos avtomatik olib tashlanishi kerak postgres dan"
            _db.MorningQuotes.Remove(quote);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "MorningQuoteJob muvaffaqiyatli yakunlandi. Iqtibos (Id={Id}, Muallif={Author}) tarqatildi va PostgreSQL dan olib tashlandi.",
                quote.Id, quote.Author);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MorningQuoteJob bajarilishida xatolik yuz berdi (Iqtibos Id={Id}).", quote.Id);
        }
    }
}

