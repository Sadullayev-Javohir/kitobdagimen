using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Analytics;

/// <summary>
/// Computes founder-level product analytics from the operational tables. There is no dedicated
/// event-log table, so "activity" is derived from the timestamps that already exist: posts,
/// quotes, comments, likes, chat messages, stories, daily reading progress, and the signup itself.
/// A user counts as active on a day if they produced any of those on that day.
///
/// All work is bounded to a lookback window (max of the 30-day chart and the 8-week retention
/// range) so the in-memory join stays small. Days are bucketed in UTC+5 (Uzbekistan), matching
/// the reading leaderboard.
/// </summary>
public class GetFounderAnalyticsQueryHandler : IRequestHandler<GetFounderAnalyticsQuery, FounderAnalyticsDto>
{
    private const int UzOffsetHours = 5;
    private const int ChartDays = 30;
    private const int RetentionWeeks = 8;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFounderAnalyticsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FounderAnalyticsDto> Handle(GetFounderAnalyticsQuery request, CancellationToken ct)
    {
        // Founder-level business metrics — SuperAdmin only.
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, ct);

        var nowUtc = DateTime.UtcNow;
        var today = ToLocalDate(nowUtc);

        var chartStart = today.AddDays(-(ChartDays - 1));
        var currentWeekStart = WeekStart(today);
        var cohortStart = currentWeekStart.AddDays(-7 * (RetentionWeeks - 1));

        var earliestLocal = chartStart < cohortStart ? chartStart : cohortStart;
        // Lower bound in UTC for timestamp columns (local midnight shifted back by the offset).
        var earliestUtc = earliestLocal.ToDateTime(TimeOnly.MinValue).AddHours(-UzOffsetHours);

        // ---- Totals (all-time) ------------------------------------------
        var totalUsers = await _db.Users.CountAsync(ct);
        var totalPosts = await _db.Posts.CountAsync(ct);
        var totalQuotes = await _db.Quotes.CountAsync(ct);
        var totalMessages = await _db.Messages.CountAsync(ct);

        // ---- Signups inside the window ----------------------------------
        var signups = await _db.Users
            .Where(u => u.CreatedAt >= earliestUtc)
            .Select(u => new { u.Id, u.CreatedAt })
            .ToListAsync(ct);

        // ---- Activity events inside the window --------------------------
        // Each source is projected to (UserId, timestamp) then folded into (UserId, local day).
        var activity = new HashSet<(int UserId, DateOnly Day)>();

        async Task AddSourceAsync(IQueryable<TimedEvent> source)
        {
            var rows = await source.ToListAsync(ct);
            foreach (var r in rows)
            {
                activity.Add((r.UserId, ToLocalDate(r.At)));
            }
        }

        await AddSourceAsync(_db.Posts.Where(p => p.CreatedAt >= earliestUtc)
            .Select(p => new TimedEvent { UserId = p.UserId, At = p.CreatedAt }));
        await AddSourceAsync(_db.Quotes.Where(q => q.CreatedAt >= earliestUtc)
            .Select(q => new TimedEvent { UserId = q.UserId, At = q.CreatedAt }));
        await AddSourceAsync(_db.Comments.Where(c => c.CreatedAt >= earliestUtc)
            .Select(c => new TimedEvent { UserId = c.UserId, At = c.CreatedAt }));
        await AddSourceAsync(_db.Likes.Where(l => l.CreatedAt >= earliestUtc)
            .Select(l => new TimedEvent { UserId = l.UserId, At = l.CreatedAt }));
        await AddSourceAsync(_db.Messages.Where(m => m.SentAt >= earliestUtc)
            .Select(m => new TimedEvent { UserId = m.SenderId, At = m.SentAt }));
        await AddSourceAsync(_db.Stories.Where(s => s.CreatedAt >= earliestUtc)
            .Select(s => new TimedEvent { UserId = s.UserId, At = s.CreatedAt }));

        // Reading progress already stores a local DateOnly; join to get the owner id.
        var readingRows = await _db.ReadingProgress
            .Where(rp => rp.Date >= earliestLocal)
            .Select(rp => new { UserId = rp.ReadingGoal.UserId, rp.Date })
            .ToListAsync(ct);
        foreach (var r in readingRows)
        {
            activity.Add((r.UserId, r.Date));
        }

        // The signup itself counts as activity on the signup day.
        foreach (var s in signups)
        {
            activity.Add((s.Id, ToLocalDate(s.CreatedAt)));
        }

        // ---- Engagement (DAU / WAU / MAU) -------------------------------
        var dau = activity.Where(a => a.Day == today).Select(a => a.UserId).Distinct().Count();
        var wau = DistinctUsersBetween(activity, today.AddDays(-6), today);
        var mau = DistinctUsersBetween(activity, today.AddDays(-29), today);
        var stickiness = mau > 0 ? (double)dau / mau * 100 : 0;

        // ---- Growth (signups) -------------------------------------------
        int NewUsersBetween(DateOnly from, DateOnly to) =>
            signups.Count(s =>
            {
                var d = ToLocalDate(s.CreatedAt);
                return d >= from && d <= to;
            });

        var newToday = NewUsersBetween(today, today);
        var new7d = NewUsersBetween(today.AddDays(-6), today);
        var new30d = NewUsersBetween(today.AddDays(-29), today);
        var prev7d = NewUsersBetween(today.AddDays(-13), today.AddDays(-7));
        var growth = prev7d > 0
            ? (double)(new7d - prev7d) / prev7d * 100
            : (new7d > 0 ? 100 : 0);

        // ---- Daily activity series --------------------------------------
        var newByDay = signups
            .GroupBy(s => ToLocalDate(s.CreatedAt))
            .ToDictionary(g => g.Key, g => g.Count());
        var activeByDay = activity
            .GroupBy(a => a.Day)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

        var daily = new List<DailyActivityPoint>();
        for (var day = chartStart; day <= today; day = day.AddDays(1))
        {
            daily.Add(new DailyActivityPoint(
                day,
                activeByDay.TryGetValue(day, out var au) ? au : 0,
                newByDay.TryGetValue(day, out var nu) ? nu : 0));
        }

        // ---- Conversion funnel ------------------------------------------
        var onboarded = await _db.UserGenres.Select(ug => ug.UserId).Distinct().CountAsync(ct);
        var activated = await _db.Posts.Select(p => p.UserId)
            .Union(_db.Quotes.Select(q => q.UserId))
            .Union(_db.ReadingGoals.Select(g => g.UserId))
            .Distinct()
            .CountAsync(ct);
        var active7d = wau;

        var funnel = BuildFunnel(totalUsers, onboarded, activated, active7d);

        // ---- Retention cohorts ------------------------------------------
        var retention = BuildRetention(signups.Select(s => (s.Id, ToLocalDate(s.CreatedAt))),
            activity, cohortStart, currentWeekStart);

        return new FounderAnalyticsDto
        {
            GeneratedAtUtc = nowUtc,
            TotalUsers = totalUsers,
            NewUsersToday = newToday,
            NewUsers7d = new7d,
            NewUsers30d = new30d,
            SignupGrowthPercent = Math.Round(growth, 1),
            Dau = dau,
            Wau = wau,
            Mau = mau,
            StickinessPercent = Math.Round(stickiness, 1),
            TotalPosts = totalPosts,
            TotalQuotes = totalQuotes,
            TotalMessages = totalMessages,
            DailyActivity = daily,
            Funnel = funnel,
            Retention = retention,
            RetentionWeekCount = RetentionWeeks
        };
    }

    private static List<FunnelStep> BuildFunnel(int total, int onboarded, int activated, int active7d)
    {
        var steps = new (string Key, string Label, int Count)[]
        {
            ("registered", "Ro'yxatdan o'tgan", total),
            ("onboarded", "Janr tanlagan (onboarding)", onboarded),
            ("activated", "Kontent yaratgan", activated),
            ("active", "So'nggi 7 kunda faol", active7d)
        };

        var result = new List<FunnelStep>();
        for (var i = 0; i < steps.Length; i++)
        {
            var (key, label, count) = steps[i];
            var pctTop = total > 0 ? (double)count / total * 100 : 0;
            var prev = i == 0 ? count : steps[i - 1].Count;
            var pctPrev = prev > 0 ? (double)count / prev * 100 : 0;
            result.Add(new FunnelStep(key, label, count, Math.Round(pctTop, 1), Math.Round(pctPrev, 1)));
        }
        return result;
    }

    private static List<RetentionCohort> BuildRetention(
        IEnumerable<(int UserId, DateOnly SignupDay)> signups,
        HashSet<(int UserId, DateOnly Day)> activity,
        DateOnly cohortStart,
        DateOnly currentWeekStart)
    {
        // Per-user set of weeks in which they were active.
        var userWeeks = new Dictionary<int, HashSet<DateOnly>>();
        foreach (var (userId, day) in activity)
        {
            var w = WeekStart(day);
            if (!userWeeks.TryGetValue(userId, out var set))
            {
                set = new HashSet<DateOnly>();
                userWeeks[userId] = set;
            }
            set.Add(w);
        }

        // Group signups into weekly cohorts.
        var cohortUsers = new Dictionary<DateOnly, List<int>>();
        foreach (var (userId, signupDay) in signups)
        {
            var w = WeekStart(signupDay);
            if (w < cohortStart)
            {
                continue;
            }
            if (!cohortUsers.TryGetValue(w, out var list))
            {
                list = new List<int>();
                cohortUsers[w] = list;
            }
            list.Add(userId);
        }

        var cohorts = new List<RetentionCohort>();
        for (var i = 0; i < RetentionWeeks; i++)
        {
            var cw = cohortStart.AddDays(7 * i);
            var users = cohortUsers.TryGetValue(cw, out var list) ? list : new List<int>();
            var size = users.Count;
            var maxOffset = (int)((currentWeekStart.DayNumber - cw.DayNumber) / 7);

            var cells = new List<double?>();
            for (var k = 0; k < RetentionWeeks; k++)
            {
                if (k > maxOffset || size == 0)
                {
                    cells.Add(null);
                    continue;
                }
                var target = cw.AddDays(7 * k);
                var retained = users.Count(u => userWeeks.TryGetValue(u, out var set) && set.Contains(target));
                cells.Add(Math.Round((double)retained / size * 100, 0));
            }

            cohorts.Add(new RetentionCohort(FormatWeekLabel(cw), cw, size, cells));
        }
        return cohorts;
    }

    private static int DistinctUsersBetween(HashSet<(int UserId, DateOnly Day)> activity, DateOnly from, DateOnly to) =>
        activity.Where(a => a.Day >= from && a.Day <= to).Select(a => a.UserId).Distinct().Count();

    private static DateOnly ToLocalDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(UzOffsetHours));

    /// <summary>Monday-based start of the week containing <paramref name="d"/>.</summary>
    private static DateOnly WeekStart(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    private static readonly string[] UzMonths =
    {
        "yan", "fev", "mar", "apr", "may", "iyun", "iyul", "avg", "sen", "okt", "noy", "dek"
    };

    private static string FormatWeekLabel(DateOnly weekStart) =>
        $"{weekStart.Day}-{UzMonths[weekStart.Month - 1]}";

    /// <summary>Flat projection used to unify the various timestamped tables.</summary>
    private sealed class TimedEvent
    {
        public int UserId { get; set; }
        public DateTime At { get; set; }
    }
}
