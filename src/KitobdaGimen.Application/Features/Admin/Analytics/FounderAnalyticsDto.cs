namespace KitobdaGimen.Application.Features.Admin.Analytics;

/// <summary>
/// Founder-level product analytics: growth (signups), engagement (DAU/WAU/MAU),
/// activation/conversion funnel, and weekly retention cohorts. Aggregate numbers only —
/// no PII. Days are bucketed in Uzbekistan local time (UTC+5), consistent with the rest
/// of the app (e.g. reading leaderboard).
/// </summary>
public record FounderAnalyticsDto
{
    public DateTime GeneratedAtUtc { get; init; }

    // ---- Growth ---------------------------------------------------------
    public int TotalUsers { get; init; }
    public int NewUsersToday { get; init; }
    public int NewUsers7d { get; init; }
    public int NewUsers30d { get; init; }

    /// <summary>Week-over-week signup growth (%): last 7 days vs the previous 7 days.</summary>
    public double SignupGrowthPercent { get; init; }

    // ---- Engagement -----------------------------------------------------
    /// <summary>Daily active users — distinct users with any tracked action today.</summary>
    public int Dau { get; init; }

    /// <summary>Weekly active users — distinct active users in the last 7 days.</summary>
    public int Wau { get; init; }

    /// <summary>Monthly active users — distinct active users in the last 30 days.</summary>
    public int Mau { get; init; }

    /// <summary>Stickiness = DAU / MAU (%). Higher means users come back more often.</summary>
    public double StickinessPercent { get; init; }

    // ---- Content volume (context) --------------------------------------
    public int TotalPosts { get; init; }
    public int TotalQuotes { get; init; }
    public int TotalMessages { get; init; }

    // ---- Series & tables ------------------------------------------------
    /// <summary>Last 30 days: active users and new signups per day (oldest first).</summary>
    public IReadOnlyList<DailyActivityPoint> DailyActivity { get; init; } = Array.Empty<DailyActivityPoint>();

    /// <summary>Registration → onboarding → activation → weekly-active funnel.</summary>
    public IReadOnlyList<FunnelStep> Funnel { get; init; } = Array.Empty<FunnelStep>();

    /// <summary>Weekly signup cohorts with per-week return rates (oldest first).</summary>
    public IReadOnlyList<RetentionCohort> Retention { get; init; } = Array.Empty<RetentionCohort>();

    /// <summary>Number of week columns rendered in the retention grid.</summary>
    public int RetentionWeekCount { get; init; }
}

/// <summary>One day on the activity chart.</summary>
public record DailyActivityPoint(DateOnly Date, int ActiveUsers, int NewUsers);

/// <summary>One step of the conversion funnel.</summary>
public record FunnelStep(string Key, string Label, int Count, double PercentOfTop, double PercentOfPrevious);

/// <summary>
/// A weekly signup cohort. <see cref="WeeklyRetention"/>[k] is the % of the cohort that was
/// active in the k-th week after signup (k=0 is the signup week). Null means that week has
/// not happened yet.
/// </summary>
public record RetentionCohort(string Label, DateOnly WeekStart, int CohortSize, IReadOnlyList<double?> WeeklyRetention);
