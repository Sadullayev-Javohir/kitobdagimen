using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds demo content at application startup.
/// Best-effort: if the database is unreachable (common in local/dev runs without
/// Postgres) the failure is logged and the app continues to start. Canonical
/// genres are seeded through EF <c>HasData</c> (migrations); this only adds a
/// handful of sample books, and only when the Books table is still empty.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
        var db = sp.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.MigrateAsync(ct);
            await SeedBooksAsync(db, logger, ct);
            await CleanupPreStartWinnersAsync(db, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Ma'lumotlar bazasini migratsiya/seed qilib bo'lmadi — startup davom etadi. " +
                "Postgres ulanishini tekshiring.");
        }
    }

    private static async Task SeedBooksAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Books.AnyAsync(ct))
        {
            return;
        }

        // GenreId qiymatlari GenreConfiguration.HasData bilan mos (1..10).
        var books = new[]
        {
            new Book { Title = "O'tkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 384, GenreId = 1 },
            new Book { Title = "Mehrobdan chayon", Author = "Abdulla Qodiriy", TotalPages = 352, GenreId = 1 },
            new Book { Title = "Kecha va kunduz", Author = "Cho'lpon", TotalPages = 272, GenreId = 1 },
            new Book { Title = "Sariq devni minib", Author = "Xudoyberdi To'xtaboyev", TotalPages = 320, GenreId = 9 },
            new Book { Title = "Ufq", Author = "Said Ahmad", TotalPages = 448, GenreId = 1 },
            new Book { Title = "Sapiens: Insoniyatning qisqacha tarixi", Author = "Yuval Noa Harari", TotalPages = 443, GenreId = 8 },
            new Book { Title = "Tafakkur: tez va sekin", Author = "Daniel Kaneman", TotalPages = 499, GenreId = 10 },
            new Book { Title = "Boy ota, kambag'al ota", Author = "Robert Kiyosaki", TotalPages = 336, GenreId = 6 },
            new Book { Title = "Sherlok Xolmsning sarguzashtlari", Author = "Artur Konan Doyl", TotalPages = 307, GenreId = 3 },
            new Book { Title = "Atom odatlari", Author = "Jeyms Klir", TotalPages = 320, GenreId = 10 },
        };

        db.Books.AddRange(books);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("{Count} ta namuna kitob seed qilindi.", books.Length);
    }

    /// <summary>
    /// Challenge boshlanishidan (Iyul 2026) oldingi oylar uchun eski g'olib yozuvlarini o'chiradi
    /// (masalan May/Iyun). Idempotent — keyingi startuplarda o'chiradigan narsa qolmaydi.
    /// G'olibga bog'liq like yozuvlari DB darajasidagi cascade orqali o'chiriladi.
    /// </summary>
    private static async Task CleanupPreStartWinnersAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var (sy, sm) = Application.Common.ChallengeCalendar.StartPeriod;

        var removed = await db.ChallengeWinners
            .Where(w => w.Year < sy || (w.Year == sy && w.Month < sm))
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            logger.LogInformation(
                "Challenge boshlanishidan oldingi {Count} ta g'olib yozuvi o'chirildi.", removed);
        }
    }
}
