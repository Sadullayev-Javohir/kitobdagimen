using System.Text.Json;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Home.Queries.GetLandingStats;

public class GetLandingStatsQueryHandler : IRequestHandler<GetLandingStatsQuery, LandingStatsDto>
{
    /// <summary>Toshkent vaqti (UTC+5, DSTsiz) — "kun" chegarasi shu zonada hisoblanadi.</summary>
    private static readonly TimeSpan UzOffset = TimeSpan.FromHours(5);

    private readonly IAppDbContext _db;

    public GetLandingStatsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<LandingStatsDto> Handle(GetLandingStatsQuery request, CancellationToken ct)
    {
        var todayUz = UzDate(DateTime.UtcNow);

        if (!request.ForceRefresh)
        {
            var cachedJson = await _db.AppSettings
                .Where(s => s.Key == AppSettingKeys.LandingStats)
                .Select(s => s.Value)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                LandingStatsDto? cached = null;
                try { cached = JsonSerializer.Deserialize<LandingStatsDto>(cachedJson); }
                catch (JsonException) { /* buzilgan snapshot — qayta hisoblaymiz */ }

                // Snapshot bugunga tegishli bo'lsa — shuni qaytaramiz (kunlik yangilanish).
                if (cached is not null && UzDate(cached.UpdatedAtUtc) == todayUz)
                {
                    return cached;
                }
            }
        }

        var fresh = new LandingStatsDto
        {
            UserCount = await _db.Users.CountAsync(ct),
            BooksRead = await _db.ReadingGoals
                .CountAsync(g => g.Book.TotalPages > 0 && g.CurrentPage >= g.Book.TotalPages, ct),
            PagesRead = await _db.ReadingProgress
                .Where(p => p.PagesReadToday > 0)
                .SumAsync(p => (long)p.PagesReadToday, ct),
            UpdatedAtUtc = DateTime.UtcNow
        };

        await SaveSnapshotAsync(fresh, ct);
        return fresh;
    }

    private static DateOnly UzDate(DateTime utc) => DateOnly.FromDateTime(utc + UzOffset);

    private async Task SaveSnapshotAsync(LandingStatsDto dto, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(dto);
        var setting = await _db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == AppSettingKeys.LandingStats, ct);

        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKeys.LandingStats,
                Value = json,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = json;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
