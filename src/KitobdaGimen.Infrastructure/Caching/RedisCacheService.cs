using System.Text.Json;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace KitobdaGimen.Infrastructure.Caching;

/// <summary>
/// Redis-backed <see cref="ICacheService"/>. Cache failures are swallowed and logged so that a
/// Redis outage degrades gracefully instead of breaking requests.
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value!, JsonOptions) : default;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis o'qishda xatolik (key: {Key})", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _redis.GetDatabase().StringSetAsync(key, json, expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis yozishda xatolik (key: {Key})", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis o'chirishda xatolik (key: {Key})", key);
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsReplica)
                {
                    await server.FlushDatabaseAsync();
                    _logger.LogInformation("Redis keshlari tozalandi (endpoint: {Endpoint})", endpoint);
                }
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis tozalashda xatolik");
        }
    }
}
