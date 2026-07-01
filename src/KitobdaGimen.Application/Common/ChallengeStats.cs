using KitobdaGimen.Application.Features.Challenge.Dtos;

namespace KitobdaGimen.Application.Common;

/// <summary>
/// O'qish yozuvlaridan (sana + betlar + kitob id) statistika tuzuvchi yagona yordamchi.
/// GitHub uslubidagi yillik kalendar (heatmap) va kunlik statistikani hisoblaydi.
/// </summary>
public static class ChallengeStats
{
    /// <summary>Bitta o'qish yozuvi — statistika hisobi uchun soddalashtirilgan ko'rinish.</summary>
    public readonly record struct Row(DateOnly Date, int Pages, int BookId);

    /// <summary>Berilgan yil uchun to'liq kalendar (1-yanvar … 31-dekabr) tuzadi.</summary>
    public static YearCalendarDto BuildYearCalendar(IEnumerable<Row> rows, int year)
    {
        var yearRows = rows.Where(r => r.Date.Year == year).ToList();

        var perDay = yearRows
            .GroupBy(r => r.Date)
            .ToDictionary(
                g => g.Key,
                g => (Pages: g.Sum(r => r.Pages), Books: g.Select(r => r.BookId).Distinct().Count()));

        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        var days = new List<DailyStatDto>(366);
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            perDay.TryGetValue(d, out var agg);
            days.Add(new DailyStatDto { Date = d, Pages = agg.Pages, Books = agg.Books });
        }

        return new YearCalendarDto
        {
            Year = year,
            Days = days,
            TotalPages = days.Sum(x => x.Pages),
            TotalBooks = yearRows.Select(r => r.BookId).Distinct().Count(),
            ActiveDays = days.Count(x => x.Read),
            MaxPages = days.Count > 0 ? days.Max(x => x.Pages) : 0
        };
    }
}
