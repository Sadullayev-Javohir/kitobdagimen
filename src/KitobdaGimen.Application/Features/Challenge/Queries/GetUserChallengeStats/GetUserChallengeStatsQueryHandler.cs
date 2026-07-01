using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeStats;

public class GetUserChallengeStatsQueryHandler
    : IRequestHandler<GetUserChallengeStatsQuery, UserChallengeStatsDto>
{
    private const int DailyDays = 30;

    private readonly IAppDbContext _db;

    public GetUserChallengeStatsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<UserChallengeStatsDto> Handle(
        GetUserChallengeStatsQuery request, CancellationToken cancellationToken)
    {
        var today = UzTime.Today;
        var dailyFrom = today.AddDays(-(DailyDays - 1));

        // Joriy yilning boshi — yillik kalendar uchun. Ikkala ko'rinishni qamrab oladigan
        // eng eski sanadan boshlab bir marta o'qiymiz.
        var yearStart = new DateOnly(today.Year, 1, 1);
        var earliest = dailyFrom < yearStart ? dailyFrom : yearStart;

        var rows = await _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0
                        && p.ReadingGoal.UserId == request.UserId
                        && p.Date >= earliest && p.Date <= today)
            .Select(p => new { p.Date, p.PagesReadToday, p.ReadingGoal.BookId })
            .ToListAsync(cancellationToken);

        var typedRows = rows
            .Select(r => new ChallengeStats.Row(r.Date, r.PagesReadToday, r.BookId))
            .ToList();

        // ── Kunlik (so'nggi 30 kun) ────────────────────────────────────────────────
        var dailyRows = typedRows.Where(r => r.Date >= dailyFrom).ToList();
        var perDay = dailyRows
            .GroupBy(r => r.Date)
            .ToDictionary(
                g => g.Key,
                g => (Pages: g.Sum(r => r.Pages), Books: g.Select(r => r.BookId).Distinct().Count()));

        var daily = new List<DailyStatDto>(DailyDays);
        for (var i = 0; i < DailyDays; i++)
        {
            var date = dailyFrom.AddDays(i);
            perDay.TryGetValue(date, out var agg);
            daily.Add(new DailyStatDto { Date = date, Pages = agg.Pages, Books = agg.Books });
        }

        // ── Joriy yil kalendari (GitHub heatmap) ───────────────────────────────────
        var yearCalendar = ChallengeStats.BuildYearCalendar(typedRows, today.Year);

        // ── Mavjud yillar (avvalgi yillarni ko'rish uchun) ─────────────────────────
        var firstDate = await _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0 && p.ReadingGoal.UserId == request.UserId)
            .OrderBy(p => p.Date)
            .Select(p => (DateOnly?)p.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var firstYear = firstDate?.Year ?? today.Year;
        var availableYears = new List<int>();
        for (var y = today.Year; y >= firstYear; y--)
        {
            availableYears.Add(y);
        }

        return new UserChallengeStatsDto
        {
            Daily = daily,
            MonthPages = daily.Sum(d => d.Pages),
            MonthActiveDays = daily.Count(d => d.Read),
            MonthBooks = dailyRows.Select(r => r.BookId).Distinct().Count(),
            Year = yearCalendar,
            AvailableYears = availableYears
        };
    }
}
