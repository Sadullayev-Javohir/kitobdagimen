using System.Collections.Concurrent;
using KitobdaGimen.Application.Features.Admin.Monitoring;

namespace KitobdaGimen.Web.Monitoring;

/// <summary>
/// Thread-safe HTTP traffic accumulator. <see cref="RequestMetricsMiddleware"/> records every
/// routed request here; the collector calls <see cref="Snapshot"/> once per cycle to turn the
/// accumulated samples into a <see cref="HttpMetricsSnapshot"/> over the elapsed window.
/// Only aggregate numbers and normalized paths are stored — never query strings or PII.
/// </summary>
public sealed class HttpMetrics
{
    private readonly object _gate = new();

    // Cumulative counters (process lifetime).
    private long _totalRequests;
    private long _total4xx;
    private long _total5xx;
    private long _totalRateLimited;

    // Latency samples collected since the last Snapshot() (bounded to avoid unbounded growth).
    private const int MaxLatencySamples = 4000;
    private readonly List<double> _latencies = new(1024);

    // Per-endpoint counts since the last snapshot.
    private readonly ConcurrentDictionary<string, EndpointAccumulator> _endpoints = new();

    // Window bookkeeping.
    private long _windowRequests;
    private long _window4xx;
    private long _window5xx;
    private long _windowRateLimited;
    private DateTime _windowStart = DateTime.UtcNow;

    /// <summary>Records a completed routed request.</summary>
    public void Record(string path, int statusCode, double elapsedMs)
    {
        Interlocked.Increment(ref _totalRequests);
        if (statusCode is >= 400 and < 500)
        {
            Interlocked.Increment(ref _total4xx);
            if (statusCode == 429) Interlocked.Increment(ref _totalRateLimited);
        }
        else if (statusCode >= 500)
        {
            Interlocked.Increment(ref _total5xx);
        }

        lock (_gate)
        {
            _windowRequests++;
            if (statusCode is >= 400 and < 500)
            {
                _window4xx++;
                if (statusCode == 429) _windowRateLimited++;
            }
            else if (statusCode >= 500)
            {
                _window5xx++;
            }

            if (_latencies.Count < MaxLatencySamples)
            {
                _latencies.Add(elapsedMs);
            }
        }

        var acc = _endpoints.GetOrAdd(path, _ => new EndpointAccumulator());
        acc.Add(elapsedMs);
    }

    /// <summary>Rate-limit rejections are recorded from the limiter's OnRejected callback,
    /// which fires before the request reaches the metrics middleware.</summary>
    public void RecordRateLimited()
    {
        Interlocked.Increment(ref _totalRateLimited);
        lock (_gate)
        {
            _windowRateLimited++;
        }
    }

    /// <summary>Computes aggregate metrics for the window since the previous call and resets it.</summary>
    public HttpMetricsSnapshot Snapshot()
    {
        double[] latencies;
        long windowRequests, window4xx, window5xx, windowRateLimited;
        double windowSeconds;

        lock (_gate)
        {
            var now = DateTime.UtcNow;
            windowSeconds = Math.Max(0.001, (now - _windowStart).TotalSeconds);
            _windowStart = now;

            latencies = _latencies.ToArray();
            _latencies.Clear();

            windowRequests = _windowRequests;
            window4xx = _window4xx;
            window5xx = _window5xx;
            windowRateLimited = _windowRateLimited;

            _windowRequests = 0;
            _window4xx = 0;
            _window5xx = 0;
            _windowRateLimited = 0;
        }

        double avg = 0, p95 = 0;
        if (latencies.Length > 0)
        {
            avg = latencies.Average();
            Array.Sort(latencies);
            var idx = (int)Math.Ceiling(latencies.Length * 0.95) - 1;
            idx = Math.Clamp(idx, 0, latencies.Length - 1);
            p95 = latencies[idx];
        }

        var top = _endpoints
            .Select(kv =>
            {
                var (count, totalMs) = kv.Value.Read();
                return new EndpointStat
                {
                    Path = kv.Key,
                    Count = count,
                    AvgMs = count > 0 ? totalMs / count : 0
                };
            })
            .Where(e => e.Count > 0)
            .OrderByDescending(e => e.Count)
            .Take(5)
            .ToArray();

        return new HttpMetricsSnapshot
        {
            RequestsPerSecond = windowRequests / windowSeconds,
            AvgResponseMs = avg,
            P95ResponseMs = p95,
            ErrorRate4xxPercent = windowRequests > 0 ? window4xx * 100.0 / windowRequests : 0,
            ErrorRate5xxPercent = windowRequests > 0 ? window5xx * 100.0 / windowRequests : 0,
            RateLimitedPerMinute = windowSeconds > 0 ? (long)Math.Round(windowRateLimited / windowSeconds * 60.0) : 0,
            TotalRequests = Interlocked.Read(ref _totalRequests),
            TopEndpoints = top
        };
    }

    private sealed class EndpointAccumulator
    {
        private long _count;
        private long _totalMsScaled; // ms * 1000, kept as long for interlocked accuracy

        public void Add(double elapsedMs)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _totalMsScaled, (long)(elapsedMs * 1000));
        }

        public (long Count, double TotalMs) Read()
        {
            var count = Interlocked.Read(ref _count);
            var totalMs = Interlocked.Read(ref _totalMsScaled) / 1000.0;
            return (count, totalMs);
        }
    }
}
