using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KitobdaGimen.Infrastructure.RealTime;

public static class RealTimeServiceExtensions
{
    /// <summary>
    /// Registers SignalR with a Redis backplane so hub groups and messages are fanned out
    /// across every server instance behind a load balancer. Without this, a notification or
    /// chat message raised on instance A is delivered only to clients connected to A — users
    /// connected to instance B never receive the live push, even though the row is persisted
    /// (see <c>SignalRNotificationService</c> / <c>ChatHub</c>).
    ///
    /// The backplane opens its own Redis connection from the same connection string already used
    /// for caching/presence, with <c>AbortOnConnectFail = false</c> so a missing or down Redis
    /// does not crash startup (the site degrades to single-instance real-time behavior, matching
    /// the existing best-effort Redis philosophy).
    ///
    /// The backplane can be disabled (e.g. single-instance local/dev without Redis) via
    /// <c>"SignalR:Backplane:Enabled" = false</c>. When disabled, SignalR runs in-process only,
    /// which is correct for a single instance. It is enabled by default whenever a Redis
    /// connection string is configured.
    /// </summary>
    public static IServiceCollection AddRealTime(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddSignalR();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        var backplaneEnabled = configuration.GetValue<bool?>("SignalR:Backplane:Enabled") ?? true;

        if (backplaneEnabled && !string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.AddStackExchangeRedis(options =>
            {
                // Reuse the same Redis endpoint; match AddCaching's abort-on-fail-off behavior so
                // a missing Redis degrades gracefully instead of crashing startup.
                options.Configuration = ConfigurationOptions.Parse(redisConnectionString);
                options.Configuration.AbortOnConnectFail = false;
            });
        }

        return services;
    }
}
