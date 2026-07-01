namespace KitobdaGimen.Application.Features.Admin.Dtos;

/// <summary>System holati haqida ma'lumot.</summary>
public record SystemStatusDto
{
    public bool DatabaseConnected { get; init; }
    public bool RedisConnected { get; init; }
    public long MemoryUsedMb { get; init; }
    public double CpuUsagePercent { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public DateTime ServerTime { get; init; }
}
