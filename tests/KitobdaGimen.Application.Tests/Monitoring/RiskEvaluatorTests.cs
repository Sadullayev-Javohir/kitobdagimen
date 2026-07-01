using KitobdaGimen.Application.Features.Admin.Monitoring;

namespace KitobdaGimen.Application.Tests.Monitoring;

/// <summary>Unit tests for the pure health-rule evaluator that drives the admin dashboard banner.</summary>
public class RiskEvaluatorTests
{
    private static readonly MonitoringThresholds Defaults = new();

    private static (SystemMetrics, DbMetrics, RedisMetrics, HttpMetricsSnapshot, DiskMetrics) Healthy()
    {
        var system = new SystemMetrics { CpuPercent = 10, WorkingSetMb = 100, ThreadPoolQueueLength = 0 };
        var db = new DbMetrics { Up = true, PingMs = 5 };
        var redis = new RedisMetrics { Configured = true, Up = true, PingMs = 2 };
        var http = new HttpMetricsSnapshot { ErrorRate5xxPercent = 0, RateLimitedPerMinute = 0 };
        var disk = new DiskMetrics { FreePercent = 80, UploadsSizeMb = 10 };
        return (system, db, redis, http, disk);
    }

    [Fact]
    public void Healthy_metrics_produce_no_risks_and_Ok_overall()
    {
        var (system, db, redis, http, disk) = Healthy();

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Empty(risks);
        Assert.Equal(RiskLevel.Ok, overall);
    }

    [Fact]
    public void High_cpu_yields_critical()
    {
        var (system, db, redis, http, disk) = Healthy();
        system = system with { CpuPercent = 95 };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Critical, overall);
        Assert.Contains(risks, r => r.Key == "cpu" && r.Severity == RiskLevel.Critical);
    }

    [Fact]
    public void Db_down_yields_critical()
    {
        var (system, db, redis, http, disk) = Healthy();
        db = db with { Up = false };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Critical, overall);
        Assert.Contains(risks, r => r.Key == "db" && r.Severity == RiskLevel.Critical);
    }

    [Fact]
    public void Redis_down_when_configured_is_warning_not_critical()
    {
        var (system, db, redis, http, disk) = Healthy();
        redis = redis with { Up = false };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Warning, overall);
        Assert.Contains(risks, r => r.Key == "redis" && r.Severity == RiskLevel.Warning);
    }

    [Fact]
    public void Redis_not_configured_produces_no_redis_risk()
    {
        var (system, db, redis, http, disk) = Healthy();
        redis = new RedisMetrics { Configured = false, Up = false };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.DoesNotContain(risks, r => r.Key == "redis");
        Assert.Equal(RiskLevel.Ok, overall);
    }

    [Fact]
    public void Low_disk_space_yields_critical()
    {
        var (system, db, redis, http, disk) = Healthy();
        disk = disk with { FreePercent = 3 };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Critical, overall);
        Assert.Contains(risks, r => r.Key == "disk" && r.Severity == RiskLevel.Critical);
    }

    [Fact]
    public void Most_severe_risk_is_first()
    {
        var (system, db, redis, http, disk) = Healthy();
        // Warning (slow DB) + Critical (CPU) together.
        system = system with { CpuPercent = 95 };
        db = db with { PingMs = 300 };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Critical, overall);
        Assert.Equal(RiskLevel.Critical, risks[0].Severity);
    }

    [Fact]
    public void High_5xx_error_rate_yields_critical()
    {
        var (system, db, redis, http, disk) = Healthy();
        http = http with { ErrorRate5xxPercent = 10 };

        var (risks, overall) = RiskEvaluator.Evaluate(system, db, redis, http, disk, Defaults);

        Assert.Equal(RiskLevel.Critical, overall);
        Assert.Contains(risks, r => r.Key == "http5xx" && r.Severity == RiskLevel.Critical);
    }
}
