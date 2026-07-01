namespace KitobdaGimen.Application.Features.Admin.Monitoring;

/// <summary>Severity of a detected server risk. Higher value = more urgent.</summary>
public enum RiskLevel
{
    /// <summary>Normal — no action needed.</summary>
    Ok = 0,

    /// <summary>Worth attention — approaching a limit or a recoverable degradation.</summary>
    Warning = 1,

    /// <summary>Needs immediate attention — a limit breached or a component down.</summary>
    Critical = 2
}

/// <summary>One evaluated health rule. Titles/details are in Uzbek for the admin UI.</summary>
public record RiskItem
{
    /// <summary>Stable identifier (e.g. "cpu", "db", "redis") — used as the UI key.</summary>
    public string Key { get; init; } = "";

    public RiskLevel Severity { get; init; }

    /// <summary>Short Uzbek title shown in the risk list.</summary>
    public string Title { get; init; } = "";

    /// <summary>Uzbek explanation with the measured value vs. threshold.</summary>
    public string Detail { get; init; } = "";
}
