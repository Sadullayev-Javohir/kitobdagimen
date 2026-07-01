namespace KitobdaGimen.Application.Features.Admin.Monitoring;

/// <summary>
/// Tunable limits for <see cref="RiskEvaluator"/>. Bound from the <c>Monitoring:Thresholds</c>
/// configuration section in the Web layer; defaults here are used when config is absent.
/// </summary>
public class MonitoringThresholds
{
    public double CpuWarn { get; set; } = 75;
    public double CpuCrit { get; set; } = 90;

    /// <summary>Working-set memory warn/crit, as a percentage of <see cref="MemoryBudgetMb"/>.</summary>
    public double MemWarnPct { get; set; } = 75;
    public double MemCritPct { get; set; } = 90;

    /// <summary>Assumed process memory budget (MB) used to turn working-set into a percentage.</summary>
    public long MemoryBudgetMb { get; set; } = 1024;

    public long ThreadPoolQueueWarn { get; set; } = 100;
    public long ThreadPoolQueueCrit { get; set; } = 1000;

    public double DbPingWarnMs { get; set; } = 200;

    public double RedisPingWarnMs { get; set; } = 100;

    public double Http5xxWarnPct { get; set; } = 1;
    public double Http5xxCritPct { get; set; } = 5;

    public long RateLimitWarnPerMin { get; set; } = 50;
    public long RateLimitCritPerMin { get; set; } = 500;

    public double DiskFreeWarnPct { get; set; } = 15;
    public double DiskFreeCritPct { get; set; } = 5;

    public long UploadsWarnMb { get; set; } = 5000;
}
