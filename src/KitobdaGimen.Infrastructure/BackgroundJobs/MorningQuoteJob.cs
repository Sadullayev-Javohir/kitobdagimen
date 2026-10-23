using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Har kuni ertalab soat 05:30 da (O'zbekiston vaqti, UTC 00:30) ishga tushadigan fon vazifasi.
/// Bazadagi navbatdagi (eng birinchi turgan) erta tonggi motivatsion iqtibosni olib,
/// faqatgina Telegram guruhiga va bot foydalanuvchilariga e'lon qiladi (veb saytga yuborilmaydi).
/// E'lon qilingandan so'ng, ushbu iqtibos avtomatik ravishda PostgreSQL ma'lumotlar bazasidan
/// o'chirib tashlanadi (FIFO navbat mexanizmi).
/// </summary>
public class MorningQuoteJob
{
    public const string RecurringJobId = "daily-morning-quote";
    public const string NotificationType = "morning_quote";

    private readonly IAppDbContext _db;
    private readonly ITelegramBotService _telegramBot;
    private readonly ILogger<MorningQuoteJob> _logger;

    public MorningQuoteJob(
        IAppDbContext db,
        ITelegramBotService telegramBot,
        ILogger<MorningQuoteJob> logger)
    {
        _db = db;
        _telegramBot = telegramBot;
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
            // 1. Telegram guruhiga va bot orqali foydalanuvchilarga yuborish (veb saytga emas)
            await _telegramBot.BroadcastMorningQuoteAsync(
                quote.Text, quote.Author, quote.Source, cancellationToken);

            // 2. Talab: "birinchi qo'yilgan iqtibos avtomatik olib tashlanishi kerak postgres dan"
            _db.MorningQuotes.Remove(quote);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "MorningQuoteJob muvaffaqiyatli yakunlandi. Iqtibos (Id={Id}, Muallif={Author}) Telegram bot va guruhlarga tarqatildi va PostgreSQL dan olib tashlandi.",
                quote.Id, quote.Author);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MorningQuoteJob bajarilishida xatolik yuz berdi (Iqtibos Id={Id}).", quote.Id);
        }
    }
}

