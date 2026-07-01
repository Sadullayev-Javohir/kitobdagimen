namespace KitobdaGimen.Application.Features.Admin.Monitoring;

/// <summary>
/// A single point-in-time picture of the server's health. Built by the metrics collector in the
/// Web layer (every few seconds), stored in a ring buffer and pushed to admins over SignalR.
/// Contains ONLY aggregate numbers and states — never secrets (connection strings, keys) or PII.
/// </summary>
public record ServerSnapshot
{
    public DateTime Timestamp { get; init; }
    public SystemMetrics System { get; init; } = new();
    public DbMetrics Database { get; init; } = new();
    public RedisMetrics Redis { get; init; } = new();
    public RealtimeMetrics Realtime { get; init; } = new();
    public HangfireMetrics Hangfire { get; init; } = new();
    public HttpMetricsSnapshot Http { get; init; } = new();
    public DiskMetrics Disk { get; init; } = new();

    /// <summary>All evaluated risks (including Ok ones can be filtered client-side).</summary>
    public IReadOnlyList<RiskItem> Risks { get; init; } = Array.Empty<RiskItem>();

    /// <summary>The highest severity across <see cref="Risks"/> — drives the global banner colour.</summary>
    public RiskLevel OverallRisk { get; init; }
}

/// <summary>Process / runtime metrics.</summary>
public record SystemMetrics
{
    public double CpuPercent { get; init; }
    public long WorkingSetMb { get; init; }
    public long ManagedHeapMb { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public int ThreadPoolBusyWorkers { get; init; }
    public int ThreadPoolMaxWorkers { get; init; }
    public long ThreadPoolQueueLength { get; init; }
    public double UptimeSeconds { get; init; }
    public string Uptime { get; init; } = "";
    public string DotNetVersion { get; init; } = "";
    public string OsDescription { get; init; } = "";
    public string MachineName { get; init; } = "";
    public int ProcessorCount { get; init; }
}

/// <summary>PostgreSQL connectivity and load.</summary>
public record DbMetrics
{
    public bool Up { get; init; }
    public double PingMs { get; init; }
    public int ActiveConnections { get; init; }
    public double LongestQuerySeconds { get; init; }
    public long DatabaseSizeMb { get; init; }
}

/// <summary>Redis connectivity. When not configured/unreachable the site still runs (degraded).</summary>
public record RedisMetrics
{
    public bool Configured { get; init; }
    public bool Up { get; init; }
    public double PingMs { get; init; }
    public string UsedMemory { get; init; } = "";
    public int ConnectedClients { get; init; }
}

/// <summary>Live presence and SignalR connection counts.</summary>
public record RealtimeMetrics
{
    public int OnlineUsers { get; init; }
    public int ActiveConnections { get; init; }
}

/// <summary>Hangfire background-job statistics.</summary>
public record HangfireMetrics
{
    public bool Available { get; init; }
    public long Enqueued { get; init; }
    public long Scheduled { get; init; }
    public long Processing { get; init; }
    public long Succeeded { get; init; }
    public long Failed { get; init; }
    public int Servers { get; init; }
    public IReadOnlyList<string> RecentFailed { get; init; } = Array.Empty<string>();
}

/// <summary>HTTP traffic, sampled by the request-metrics middleware over a sliding window.</summary>
public record HttpMetricsSnapshot
{
    public double RequestsPerSecond { get; init; }
    public double AvgResponseMs { get; init; }
    public double P95ResponseMs { get; init; }
    public double ErrorRate4xxPercent { get; init; }
    public double ErrorRate5xxPercent { get; init; }
    public long RateLimitedPerMinute { get; init; }
    public long TotalRequests { get; init; }
    public IReadOnlyList<EndpointStat> TopEndpoints { get; init; } = Array.Empty<EndpointStat>();
}

/// <summary>Per-endpoint aggregate for the "busiest routes" list.</summary>
public record EndpointStat
{
    public string Path { get; init; } = "";
    public long Count { get; init; }
    public double AvgMs { get; init; }
}

/// <summary>Uploads folder size and host drive free space.</summary>
public record DiskMetrics
{
    public long UploadsSizeMb { get; init; }
    public long DriveFreeMb { get; init; }
    public long DriveTotalMb { get; init; }
    public double FreePercent { get; init; }
}
