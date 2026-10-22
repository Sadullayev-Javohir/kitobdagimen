using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Har kuni 23:59 da O'zbekiston vaqti bilan ishga tushadigan fon vazifasi.
/// O'sha kuni yozilgan taqriz va iqtiboslar orasidan tasodifiy (random) bittasini
/// yangi kun uchun "Kun taqrizi" yoki "Kun iqtibosi" (Jackpot) sifatida tanlaydi
/// va Telegram guruhiga e'lon qiladi.
/// </summary>
public class DailyJackpotJob
{
    public const string RecurringJobId = "daily-jackpot-selection";

    private readonly IDailyJackpotService _jackpotService;
    private readonly ILogger<DailyJackpotJob> _logger;

    public DailyJackpotJob(IDailyJackpotService jackpotService, ILogger<DailyJackpotJob> logger)
    {
        _jackpotService = jackpotService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Bugungi sana Toshkent vaqti bilan
        var today = UzTime.Today;
        var tomorrow = today.AddDays(1);

        _logger.LogInformation("DailyJackpotJob ishga tushdi: {Today} yakunlanmoqda, {Tomorrow} uchun yangi g'olib tanlanadi.", today, tomorrow);

        try
        {
            // Yangi kun uchun g'olibni tanlash va Telegram guruhiga e'lon qilish
            var jackpot = await _jackpotService.PickJackpotForDateAsync(tomorrow, broadcast: true, cancellationToken);
            if (jackpot is not null)
            {
                _logger.LogInformation("DailyJackpotJob muvaffaqiyatli yakunlandi: Sana={Date}, Muallif={User}, Tur={Type}",
                    jackpot.Date, jackpot.UserFullName, jackpot.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DailyJackpotJob bajarilishida xatolik yuz berdi.");
        }
    }
}
