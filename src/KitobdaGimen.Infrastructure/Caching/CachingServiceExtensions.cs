using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KitobdaGimen.Infrastructure.Caching;

public static class CachingServiceExtensions
{
    /// <summary>
    /// Registers Redis (a single shared multiplexer) and the <see cref="ICacheService"/>.
    /// Connects with <c>AbortOnConnectFail = false</c> so a missing Redis does not crash startup.
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
