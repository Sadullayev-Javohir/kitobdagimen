using System.Data;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Admin.Monitoring;
using KitobdaGimen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace KitobdaGimen.Web.Monitoring;

/// <summary>
/// Central background collector: every <c>Monitoring:IntervalSeconds</c> it gathers one
/// <see cref="ServerSnapshot"/> (system, DB, Redis, real-time, Hangfire, HTTP, disk), evaluates
/// risks and stores it in the singleton <see cref="IServerMetricsStore"/>. A single collector
/// serves every admin viewer, so the DB/Redis load stays constant regardless of how many admins
/// have the panel open. Every source is wrapped in its own try/catch — one failing source never
/// stops the others (best-effort, same style as the presence service).
/// Heavy queries (pg database size, uploads folder size) run on a slower cadence and are cached.
/// </summary>
public sealed class MetricsCollectorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServerMetricsStore _store;
    private readonly SystemMetricsReader _system;
    private readonly HttpMetrics _http;
    private readonly RealtimeConnectionCounter _connections;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly MonitoringThresholds _thresholds;

    private readonly TimeSpan _interval;
    private readonly TimeSpan _heavyInterval;
    private readonly bool _redisConfigured;

    // Cached heavy metrics.
    private DateTime _lastHeavyUtc = DateTime.MinValue;
    private long _cachedDbSizeMb;
    private long _cachedUploadsMb;
    private long _cachedDriveFreeMb;
    private long _cachedDriveTotalMb;

    public MetricsCollectorService(
        IServiceScopeFactory scopeFactory,
        IServerMetricsStore store,
        SystemMetricsReader system,
        HttpMetrics http,
        RealtimeConnectionCounter connections,
        IConfiguration config,
        IOptions<MonitoringThresholds> thresholds,
        ILogger<MetricsCollectorService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _scopeFactory = scopeFactory;
        _store = store;
        _system = system;
        _http = http;
        _connections = connections;
        _config = config;
        _thresholds = thresholds.Value;
        _logger = logger;
        _redis = redis;

        var intervalSeconds = config.GetValue<double?>("Monitoring:IntervalSeconds") ?? 3.0;
        var heavySeconds = config.GetValue<double?>("Monitoring:HeavyIntervalSeconds") ?? 60.0;
        _interval = TimeSpan.FromSeconds(Math.Max(1.0, intervalSeconds));
        _heavyInterval = TimeSpan.FromSeconds(Math.Max(10.0, heavySeconds));

        var redisConn = config.GetConnectionString("Redis");
        _redisConfigured = !string.IsNullOrWhiteSpace(redisConn);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Collect once immediately so the panel is populated right after startup.
        await CollectSafeAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CollectSafeAsync(stoppingToken);
        }
    }

    private async Task CollectSafeAsync(CancellationToken ct)
    {
        try
        {
            var snapshot = await CollectAsync(ct);
            _store.Add(snapshot);
        }
        catch (OperationCanceledException)
        {
            // Shutting down — ignore.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metrikalarni yig'ishda kutilmagan xato (snapshot o'tkazib yuborildi).");
        }
    }

    private async Task<ServerSnapshot> CollectAsync(CancellationToken ct)
    {
        var runHeavy = DateTime.UtcNow - _lastHeavyUtc >= _heavyInterval;

        var system = ReadSystem();
        var (db, dbActive, dbLongest) = await ReadDatabaseAsync(runHeavy, ct);
        var redis = await ReadRedisAsync();
        var realtime = await ReadRealtimeAsync();
        var hangfire = ReadHangfire();
        var http = _http.Snapshot();
        var disk = ReadDisk(runHeavy);

        if (runHeavy)
        {
            _lastHeavyUtc = DateTime.UtcNow;
        }

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, _thresholds);

        return new ServerSnapshot
        {
            Timestamp = DateTime.UtcNow,
            System = system,
            Database = db,
            Redis = redis,
            Realtime = realtime,
            Hangfire = hangfire,
            Http = http,
            Disk = disk,
            Risks = risks,
            OverallRisk = overall
        };
    }

    private SystemMetrics ReadSystem()
    {
        try
        {
            return _system.Read();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "System metrikalarini o'qishda xato.");
            return new SystemMetrics();
        }
    }

    private async Task<(DbMetrics Metrics, int Active, double Longest)> ReadDatabaseAsync(bool runHeavy, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = context.Database.GetDbConnection();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var canConnect = await context.Database.CanConnectAsync(ct);
            sw.Stop();

            if (!canConnect)
            {
                return (new DbMetrics { Up = false }, 0, 0);
            }

            var pingMs = sw.Elapsed.TotalMilliseconds;
            int activeConnections = 0;
            double longestSeconds = 0;
            long dbSizeMb = _cachedDbSizeMb;

            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(ct);
                }

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT count(*)::int, " +
                        "COALESCE(EXTRACT(EPOCH FROM max(now() - query_start)) FILTER (WHERE state = 'active'), 0)::float8 " +
                        "FROM pg_stat_activity WHERE datname = current_database()";
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        activeConnections = reader.GetInt32(0);
                        longestSeconds = reader.GetDouble(1);
                    }
                }

                if (runHeavy)
                {
                    await using var sizeCmd = connection.CreateCommand();
                    sizeCmd.CommandText = "SELECT pg_database_size(current_database())::bigint";
                    var result = await sizeCmd.ExecuteScalarAsync(ct);
                    if (result is long bytes)
                    {
                        dbSizeMb = bytes / (1024 * 1024);
                        _cachedDbSizeMb = dbSizeMb;
                    }
                }
            }
            catch (Exception ex)
            {
                // Ping succeeded but the stat query failed — still report DB up.
                _logger.LogDebug(ex, "pg_stat_activity so'rovi bajarilmadi.");
            }

            return (new DbMetrics
            {
                Up = true,
                PingMs = Math.Round(pingMs, 1),
                ActiveConnections = activeConnections,
                LongestQuerySeconds = Math.Round(longestSeconds, 1),
                DatabaseSizeMb = dbSizeMb
            }, activeConnections, longestSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB metrikalarini o'qishda xato.");
            return (new DbMetrics { Up = false }, 0, 0);
        }
    }

    private async Task<RedisMetrics> ReadRedisAsync()
    {
        if (_redis is null)
        {
            return new RedisMetrics { Configured = _redisConfigured, Up = false };
        }

        try
        {
            if (!_redis.IsConnected)
            {
                return new RedisMetrics { Configured = _redisConfigured, Up = false };
            }

            var database = _redis.GetDatabase();
            var ping = await database.PingAsync();

            string usedMemory = "";
            int connectedClients = 0;
            try
            {
                var endpoint = _redis.GetEndPoints().FirstOrDefault();
                if (endpoint is not null)
                {
                    var server = _redis.GetServer(endpoint);
                    var memoryInfo = await server.InfoAsync("memory");
                    var clientsInfo = await server.InfoAsync("clients");
                    usedMemory = FindInfoValue(memoryInfo, "used_memory_human");
                    var clientsRaw = FindInfoValue(clientsInfo, "connected_clients");
                    int.TryParse(clientsRaw, out connectedClients);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Redis INFO o'qilmadi.");
            }

            return new RedisMetrics
            {
                Configured = true,
                Up = true,
                PingMs = Math.Round(ping.TotalMilliseconds, 1),
                UsedMemory = string.IsNullOrEmpty(usedMemory) ? "—" : usedMemory,
                ConnectedClients = connectedClients
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Redis metrikalarini o'qishda xato.");
            return new RedisMetrics { Configured = _redisConfigured, Up = false };
        }
    }

    private static string FindInfoValue(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        foreach (var group in info)
        {
            foreach (var pair in group)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
        }
        return "";
    }

    private async Task<RealtimeMetrics> ReadRealtimeAsync()
    {
        int onlineUsers = 0;
        try
        {
            if (_redis is { IsConnected: true })
            {
                var endpoint = _redis.GetEndPoints().FirstOrDefault();
                if (endpoint is not null)
                {
                    var server = _redis.GetServer(endpoint);
                    await foreach (var _ in server.KeysAsync(pattern: "presence:conn:*", pageSize: 250))
                    {
                        onlineUsers++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Onlayn foydalanuvchilar sonini o'qishda xato.");
        }

        return new RealtimeMetrics
        {
            OnlineUsers = onlineUsers,
            ActiveConnections = _connections.Active
        };
    }

    private HangfireMetrics ReadHangfire()
    {
        try
        {
            var storage = Hangfire.JobStorage.Current;
            if (storage is null)
            {
                return new HangfireMetrics { Available = false };
            }

            var monitoring = storage.GetMonitoringApi();
            var stats = monitoring.GetStatistics();

            var recentFailed = new List<string>();
            try
            {
                var failed = monitoring.FailedJobs(0, 5);
                foreach (var job in failed)
                {
                    var name = job.Value?.Job is { } j ? $"{j.Type?.Name}.{j.Method?.Name}" : "—";
                    recentFailed.Add(name);
                }
            }
            catch
            {
                // Failed-job details are optional.
            }

            return new HangfireMetrics
            {
                Available = true,
                Enqueued = stats.Enqueued,
                Scheduled = stats.Scheduled,
                Processing = stats.Processing,
                Succeeded = stats.Succeeded,
                Failed = stats.Failed,
                Servers = (int)stats.Servers,
                RecentFailed = recentFailed
            };
        }
        catch (Exception ex)
        {
            // Hangfire disabled (e.g. Development) — JobStorage.Current throws. Not an error.
            _logger.LogDebug(ex, "Hangfire statistikasi mavjud emas.");
            return new HangfireMetrics { Available = false };
        }
    }

    private DiskMetrics ReadDisk(bool runHeavy)
    {
        try
        {
            if (runHeavy)
            {
                _cachedUploadsMb = ComputeUploadsSizeMb();

                var root = Path.GetPathRoot(AppContext.BaseDirectory);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady)
                    {
                        _cachedDriveFreeMb = drive.AvailableFreeSpace / (1024 * 1024);
                        _cachedDriveTotalMb = drive.TotalSize / (1024 * 1024);
                    }
                }
            }

            var freePercent = _cachedDriveTotalMb > 0
                ? _cachedDriveFreeMb * 100.0 / _cachedDriveTotalMb
                : 100.0;

            return new DiskMetrics
            {
                UploadsSizeMb = _cachedUploadsMb,
                DriveFreeMb = _cachedDriveFreeMb,
                DriveTotalMb = _cachedDriveTotalMb,
                FreePercent = Math.Round(freePercent, 1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Disk metrikalarini o'qishda xato.");
            return new DiskMetrics { FreePercent = 100.0 };
        }
    }

    private static long ComputeUploadsSizeMb()
    {
        var root = KitobdaGimen.Web.UploadPaths.Root;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            return 0;
        }

        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // File may have been deleted mid-scan — skip.
                }
            }
        }
        catch
        {
            // Directory access issue — return whatever we accumulated.
        }

        return total / (1024 * 1024);
    }
}
