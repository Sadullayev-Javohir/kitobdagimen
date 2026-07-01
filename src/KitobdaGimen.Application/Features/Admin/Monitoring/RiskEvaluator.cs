namespace KitobdaGimen.Application.Features.Admin.Monitoring;

/// <summary>
/// Pure, dependency-free health-rule evaluator. Turns the raw metric records plus tunable
/// <see cref="MonitoringThresholds"/> into a list of <see cref="RiskItem"/>s and the single
/// highest <see cref="RiskLevel"/> that drives the dashboard banner colour. Lives in the
/// Application layer so it can be unit-tested without the Web host. Uzbek titles/details.
/// </summary>
public static class RiskEvaluator
{
    public static (IReadOnlyList<RiskItem> Risks, RiskLevel Overall) Evaluate(
        SystemMetrics system,
        DbMetrics db,
        RedisMetrics redis,
        HttpMetricsSnapshot http,
        DiskMetrics disk,
        MonitoringThresholds t)
    {
        var risks = new List<RiskItem>();

        // CPU
        if (system.CpuPercent >= t.CpuCrit)
        {
            risks.Add(Risk("cpu", RiskLevel.Critical, "CPU juda band",
                $"CPU {system.CpuPercent:0.0}% (chegara {t.CpuCrit:0}%)."));
        }
        else if (system.CpuPercent >= t.CpuWarn)
        {
            risks.Add(Risk("cpu", RiskLevel.Warning, "CPU yuklamasi yuqori",
                $"CPU {system.CpuPercent:0.0}% (chegara {t.CpuWarn:0}%)."));
        }

        // Memory (working set as a percentage of the configured budget)
        var memBudget = Math.Max(1, t.MemoryBudgetMb);
        var memPct = system.WorkingSetMb * 100.0 / memBudget;
        if (memPct >= t.MemCritPct)
        {
            risks.Add(Risk("memory", RiskLevel.Critical, "Xotira tugab bormoqda",
                $"RAM {system.WorkingSetMb} MB ({memPct:0}% budjetdan)."));
        }
        else if (memPct >= t.MemWarnPct)
        {
            risks.Add(Risk("memory", RiskLevel.Warning, "Xotira sarfi yuqori",
                $"RAM {system.WorkingSetMb} MB ({memPct:0}% budjetdan)."));
        }

        // Thread pool queue (starvation)
        if (system.ThreadPoolQueueLength >= t.ThreadPoolQueueCrit)
        {
            risks.Add(Risk("threadpool", RiskLevel.Critical, "ThreadPool tiqilib qoldi",
                $"Navbat {system.ThreadPoolQueueLength} (chegara {t.ThreadPoolQueueCrit})."));
        }
        else if (system.ThreadPoolQueueLength >= t.ThreadPoolQueueWarn)
        {
            risks.Add(Risk("threadpool", RiskLevel.Warning, "ThreadPool navbati o'smoqda",
                $"Navbat {system.ThreadPoolQueueLength} (chegara {t.ThreadPoolQueueWarn})."));
        }

        // Database
        if (!db.Up)
        {
            risks.Add(Risk("db", RiskLevel.Critical, "Ma'lumotlar bazasi ulanmagan",
                "PostgreSQL ga ulanib bo'lmadi. Sayt funksiyalari ishlamasligi mumkin."));
        }
        else if (db.PingMs >= t.DbPingWarnMs)
        {
            risks.Add(Risk("db", RiskLevel.Warning, "Ma'lumotlar bazasi sekin",
                $"Ping {db.PingMs:0} ms (chegara {t.DbPingWarnMs:0} ms)."));
        }

        // Redis (degraded, not fatal — site still runs without it)
        if (redis.Configured && !redis.Up)
        {
            risks.Add(Risk("redis", RiskLevel.Warning, "Redis ulanmagan (degraded rejim)",
                "Kesh ishlamayapti — sayt sekinroq, lekin ishlaydi."));
        }
        else if (redis.Configured && redis.Up && redis.PingMs >= t.RedisPingWarnMs)
        {
            risks.Add(Risk("redis", RiskLevel.Warning, "Redis sekin javob bermoqda",
                $"Ping {redis.PingMs:0} ms (chegara {t.RedisPingWarnMs:0} ms)."));
        }

        // HTTP 5xx error rate
        if (http.ErrorRate5xxPercent >= t.Http5xxCritPct)
        {
            risks.Add(Risk("http5xx", RiskLevel.Critical, "Server xatolari ko'p (5xx)",
                $"5xx ulushi {http.ErrorRate5xxPercent:0.0}% (chegara {t.Http5xxCritPct:0}%)."));
        }
        else if (http.ErrorRate5xxPercent >= t.Http5xxWarnPct)
        {
            risks.Add(Risk("http5xx", RiskLevel.Warning, "Server xatolari kuzatilmoqda (5xx)",
                $"5xx ulushi {http.ErrorRate5xxPercent:0.0}% (chegara {t.Http5xxWarnPct:0}%)."));
        }

        // Rate-limit flood (possible DoS)
        if (http.RateLimitedPerMinute >= t.RateLimitCritPerMin)
        {
            risks.Add(Risk("ratelimit", RiskLevel.Critical, "So'rovlar toshqini (DoS gumoni)",
                $"Daqiqada {http.RateLimitedPerMinute} ta 429 (chegara {t.RateLimitCritPerMin})."));
        }
        else if (http.RateLimitedPerMinute >= t.RateLimitWarnPerMin)
        {
            risks.Add(Risk("ratelimit", RiskLevel.Warning, "Ko'p so'rovlar rad etilmoqda",
                $"Daqiqada {http.RateLimitedPerMinute} ta 429 (chegara {t.RateLimitWarnPerMin})."));
        }

        // Disk free space
        if (disk.FreePercent <= t.DiskFreeCritPct)
        {
            risks.Add(Risk("disk", RiskLevel.Critical, "Disk bo'sh joyi tugab bormoqda",
                $"Bo'sh {disk.FreePercent:0.0}% (chegara {t.DiskFreeCritPct:0}%)."));
        }
        else if (disk.FreePercent <= t.DiskFreeWarnPct)
        {
            risks.Add(Risk("disk", RiskLevel.Warning, "Disk bo'sh joyi kamaymoqda",
                $"Bo'sh {disk.FreePercent:0.0}% (chegara {t.DiskFreeWarnPct:0}%)."));
        }

        // Uploads folder size
        if (t.UploadsWarnMb > 0 && disk.UploadsSizeMb >= t.UploadsWarnMb)
        {
            risks.Add(Risk("uploads", RiskLevel.Warning, "Yuklamalar papkasi kattalashdi",
                $"Uploads {disk.UploadsSizeMb} MB (chegara {t.UploadsWarnMb} MB)."));
        }

        var overall = risks.Count == 0 ? RiskLevel.Ok : risks.Max(r => r.Severity);
        // Most severe first, so the UI can show the worst problem at the top.
        var ordered = risks.OrderByDescending(r => r.Severity).ToArray();
        return (ordered, overall);
    }

    private static RiskItem Risk(string key, RiskLevel severity, string title, string detail) => new()
    {
        Key = key,
        Severity = severity,
        Title = title,
        Detail = detail
    };
}
