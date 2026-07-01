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
    private const int MonthlyMonths = 12;

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

        // So'nggi 12 oyning boshi (shu oy ham kiradi).
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(MonthlyMonths - 1));

        // Kerakli eng eski sana — ikkala ko'rinishni qamrab oladigan.
        var earliest = dailyFrom < firstMonth ? dailyFrom : firstMonth;

        // Foydalanuvchining barcha o'qish yozuvlarini bir marta olib, xotirada guruhlaymiz.
        var rows = await _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0
                        && p.ReadingGoal.UserId == request.UserId
                        && p.Date >= earliest && p.Date <= today)
            .Select(p => new { p.Date, p.PagesReadToday, p.ReadingGoal.BookId })
            .ToListAsync(cancellationToken);

        // ── Kunlik (so'nggi 30 kun) ────────────────────────────────────────────────
        var perDay = rows
            .Where(r => r.Date >= dailyFrom)
            .GroupBy(r => r.Date)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.PagesReadToday));

        var daily = new List<DailyStatDto>(DailyDays);
        for (var i = 0; i < DailyDays; i++)
        {
            var date = dailyFrom.AddDays(i);
            perDay.TryGetValue(date, out var pages);
            daily.Add(new DailyStatDto { Date = date, Pages = pages });
        }

        // ── Oylik (so'nggi 12 oy) ──────────────────────────────────────────────────
        var perMonth = rows
            .Where(r => r.Date >= firstMonth)
            .GroupBy(r => new { r.Date.Year, r.Date.Month })
            .ToDictionary(
                g => (g.Key.Year, g.Key.Month),
                g => new
                {
                    Pages = g.Sum(r => r.PagesReadToday),
                    Books = g.Select(r => r.BookId).Distinct().Count()
                });

        var monthly = new List<MonthlyStatDto>(MonthlyMonths);
        for (var i = 0; i < MonthlyMonths; i++)
        {
            var m = firstMonth.AddMonths(i);
            perMonth.TryGetValue((m.Year, m.Month), out var agg);
            monthly.Add(new MonthlyStatDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = ChallengeCalendar.MonthName(m.Month),
                Pages = agg?.Pages ?? 0,
                Books = agg?.Books ?? 0
            });
        }

        return new UserChallengeStatsDto
        {
            Daily = daily,
            Monthly = monthly,
            MonthPages = daily.Sum(d => d.Pages),
            MonthActiveDays = daily.Count(d => d.Read),
            YearPages = monthly.Sum(m => m.Pages),
            YearActiveMonths = monthly.Count(m => m.Read)
        };
    }
}
