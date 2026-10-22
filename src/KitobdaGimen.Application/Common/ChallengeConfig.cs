using System.Text.Json;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Common;

public class ChallengePrizeBookConfig
{
    public int Rank { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? CoverUrl { get; set; }
}

public class ChallengeConfig
{
    private string _title = string.Empty;
    public string Title
    {
        get => CleanChallengeTitle(_title);
        set => _title = value ?? string.Empty;
    }
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<ChallengePrizeBookConfig> Prizes { get; set; } = new()
    {
        new ChallengePrizeBookConfig { Rank = 1, Title = "Atom odatlari", Author = "Jeyms Klir" },
        new ChallengePrizeBookConfig { Rank = 2, Title = "O'tkan kunlar", Author = "Abdulla Qodiriy" },
        new ChallengePrizeBookConfig { Rank = 3, Title = "Sapiens", Author = "Yuval Noa Harari" }
    };

    public ChallengePrizeBookConfig GetPrize(int rank)
    {
        var p = Prizes.FirstOrDefault(x => x.Rank == rank);
        if (p is not null)
        {
            return p;
        }
        p = new ChallengePrizeBookConfig { Rank = rank };
        Prizes.Add(p);
        return p;
    }

    public int TotalDays => Math.Max(1, EndDate.DayNumber - StartDate.DayNumber + 1);

    public int ElapsedDays
    {
        get
        {
            var today = UzTime.Today;
            if (today < StartDate) return 0;
            var end = today >= EndDate ? EndDate : today;
            return Math.Max(1, end.DayNumber - StartDate.DayNumber + 1);
        }
    }

    public int DaysRemaining
    {
        get
        {
            var today = UzTime.Today;
            if (today > EndDate) return 0;
            var start = today > StartDate ? today : StartDate;
            return Math.Max(0, EndDate.DayNumber - start.DayNumber + 1);
        }
    }

    public string FormattedRange =>
        $"{StartDate.Day} {ChallengeCalendar.MonthName(StartDate.Month)} — {EndDate.Day} {ChallengeCalendar.MonthName(EndDate.Month)} {EndDate.Year}";

    public static string CleanChallengeTitle(string? title) => ChallengeConfigStore.CleanChallengeTitle(title);
}

public static class ChallengeConfigStore
{
    public const string SettingKey = "Challenge:ActiveConfig";
    public const string BookClubGroupIdKey = "Telegram:BookClubGroupId";
    public const string BookClubInviteLinkKey = "Telegram:BookClubInviteLink";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static ChallengeConfig CreateDefault()
    {
        var (y, m) = ChallengeCalendar.CurrentPeriod();
        var (from, to) = ChallengeCalendar.Range(y, m);
        return new ChallengeConfig
        {
            Title = $"{ChallengeCalendar.MonthName(m)} Challenge",
            Description = "Eng ko'p kitob o'qigan TOP-3 kitobxonlar maxsus kitoblar bilan taqdirlanadi!",
            StartDate = from,
            EndDate = to,
            Prizes = new List<ChallengePrizeBookConfig>
            {
                new() { Rank = 1, Title = "Atom odatlari", Author = "Jeyms Klir" },
                new() { Rank = 2, Title = "O'tkan kunlar", Author = "Abdulla Qodiriy" },
                new() { Rank = 3, Title = "Sapiens: Insoniyatning qisqacha tarixi", Author = "Yuval Noa Harari" }
            }
        };
    }

    public static string CleanChallengeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            var (_, m) = ChallengeCalendar.CurrentPeriod();
            return $"{ChallengeCalendar.MonthName(m)} Challenge";
        }
        // Normalize "Oktabr 2026 Kitobxonlar Challenge" -> "Oktabr Challenge"
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            title,
            @"\b([A-Za-zА-Яа-яЁёʻʼ'\-]+)\s+\d{4}\s+Kitobxonlar\s+Challenge\b",
            "$1 Challenge",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+Kitobxonlar\s+Challenge\b",
            " Challenge",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\b([A-Za-zА-Яа-яЁёʻʼ'\-]+)\s+\d{4}\s+Challenge\b",
            "$1 Challenge",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    public static async Task<ChallengeConfig?> GetStoredOrNullAsync(
        IAppDbContext db, CancellationToken ct = default)
    {
        try
        {
            var row = await db.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == SettingKey, ct);

            if (row is null || string.IsNullOrWhiteSpace(row.Value))
            {
                return null;
            }

            var cfg = JsonSerializer.Deserialize<ChallengeConfig>(row.Value, JsonOpts);
            if (cfg is null || cfg.StartDate == default || cfg.EndDate == default)
            {
                return null;
            }

            EnsureThreePrizes(cfg);
            if (!string.IsNullOrWhiteSpace(cfg.Title))
            {
                cfg.Title = CleanChallengeTitle(cfg.Title);
            }
            return cfg;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<ChallengeConfig> GetOrDefaultAsync(
        IAppDbContext db, CancellationToken ct = default)
    {
        var stored = await GetStoredOrNullAsync(db, ct);
        if (stored is not null)
        {
            return stored;
        }

        var def = CreateDefault();
        EnsureThreePrizes(def);
        return def;
    }

    public static async Task SaveAsync(
        IAppDbContext db, ChallengeConfig config, CancellationToken ct = default)
    {
        EnsureThreePrizes(config);
        if (config.EndDate < config.StartDate)
        {
            config.EndDate = config.StartDate;
        }

        if (string.IsNullOrWhiteSpace(config.Title))
        {
            var (_, m) = ChallengeCalendar.CurrentPeriod();
            config.Title = $"{ChallengeCalendar.MonthName(m)} Challenge";
        }
        else
        {
            config.Title = CleanChallengeTitle(config.Title);
        }

        var json = JsonSerializer.Serialize(config, JsonOpts);
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == SettingKey, ct);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = SettingKey,
                Value = json,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static void EnsureThreePrizes(ChallengeConfig cfg)
    {
        cfg.Prizes ??= new List<ChallengePrizeBookConfig>();
        for (var r = 1; r <= 3; r++)
        {
            if (cfg.Prizes.All(p => p.Rank != r))
            {
                cfg.Prizes.Add(new ChallengePrizeBookConfig { Rank = r });
            }
        }
        cfg.Prizes = cfg.Prizes.OrderBy(p => p.Rank).ToList();
    }
}

