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
        RedisValue value;
        try
        {
            value = await _redis.GetDatabase().StringGetAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis o'qishda xatolik (key: {Key})", key);
            return default;
        }

        // A missing key returns null. Because the return type is T?, this is distinguishable
        // from a legitimately cached 0 / false for value types, so callers must treat null as
        // "not in cache" (do not coalesce to a default and assume a hit).
        if (!value.HasValue)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value!, JsonOptions);
        }
        catch (JsonException ex)
        {
            // Stored JSON was written with an incompatible T or is malformed (e.g. a non-nullable
            // value type requested from a stored JSON null). A cache read is best-effort and must
            // never break the calling request, so treat the bad entry as a miss rather than throw.
            _logger.LogWarning(ex, "Redis keshni deserializatsiya qilib bo'lmadi (key: {Key})", key);
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
