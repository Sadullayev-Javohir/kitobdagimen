using System.Diagnostics;
using System.Runtime.InteropServices;
using KitobdaGimen.Application.Features.Admin.Monitoring;

namespace KitobdaGimen.Web.Monitoring;

/// <summary>
/// Reads process/runtime metrics (CPU %, RAM, GC, thread pool, uptime). Singleton because CPU
/// usage is a delta: it remembers the previous total-processor-time sample and the wall-clock
/// timestamp, then computes the percentage of one core-second consumed since the last read,
/// normalized by <see cref="Environment.ProcessorCount"/>.
/// </summary>
public sealed class SystemMetricsReader
{
    private readonly object _gate = new();
    private TimeSpan _lastCpuTime;
    private DateTime _lastSampleUtc;
    private bool _initialized;

    public SystemMetrics Read()
    {
        var process = Process.GetCurrentProcess();

        double cpuPercent = ComputeCpuPercent(process);

        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

        ThreadPool.GetAvailableThreads(out var availableWorkers, out _);
        ThreadPool.GetMaxThreads(out var maxWorkers, out _);

        return new SystemMetrics
        {
            CpuPercent = Math.Round(cpuPercent, 1),
            WorkingSetMb = process.WorkingSet64 / (1024 * 1024),
            ManagedHeapMb = GC.GetTotalMemory(false) / (1024 * 1024),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            ThreadPoolBusyWorkers = maxWorkers - availableWorkers,
            ThreadPoolMaxWorkers = maxWorkers,
            ThreadPoolQueueLength = ThreadPool.PendingWorkItemCount,
            UptimeSeconds = uptime.TotalSeconds,
            Uptime = FormatUptime(uptime),
            DotNetVersion = RuntimeInformation.FrameworkDescription,
            OsDescription = RuntimeInformation.OSDescription,
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount
        };
    }

    private double ComputeCpuPercent(Process process)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var cpuNow = process.TotalProcessorTime;

            if (!_initialized)
            {
                _initialized = true;
                _lastCpuTime = cpuNow;
                _lastSampleUtc = now;
                return 0;
            }

            var wallMs = (now - _lastSampleUtc).TotalMilliseconds;
            var cpuMs = (cpuNow - _lastCpuTime).TotalMilliseconds;

            _lastCpuTime = cpuNow;
            _lastSampleUtc = now;

            if (wallMs <= 0) return 0;

            var cores = Math.Max(1, Environment.ProcessorCount);
            var percent = cpuMs / (wallMs * cores) * 100.0;
            return Math.Clamp(percent, 0, 100);
        }
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}k {uptime.Hours}s {uptime.Minutes}d";
        }
        if (uptime.TotalHours >= 1)
        {
            return $"{(int)uptime.TotalHours}s {uptime.Minutes}d";
        }
        return $"{uptime.Minutes}d {uptime.Seconds}s";
    }
}
