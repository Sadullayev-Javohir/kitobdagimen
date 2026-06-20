using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace KitobdaGimen.Web.RealTime;

/// <summary>
/// Redis-backed <see cref="IPresenceService"/>. Each user has a connection counter
/// <c>presence:conn:{id}</c> with a TTL refreshed by client heartbeats; a user is online while
/// the counter exists and is &gt; 0. On the last disconnect the counter is removed and the
/// user's <c>LastSeenAt</c> is persisted. All Redis access is best-effort: if Redis is down,
/// presence simply reports offline and never throws.
/// </summary>
public class RedisPresenceService : IPresenceService
{
    // A little longer than the client heartbeat interval (~30s) so a brief miss doesn't flip offline.
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(75);

    private readonly IConnectionMultiplexer _redis;
    private readonly IAppDbContext _db;
    private readonly ILogger<RedisPresenceService> _logger;

    public RedisPresenceService(
        IConnectionMultiplexer redis, IAppDbContext db, ILogger<RedisPresenceService> logger)
    {
        _redis = redis;
        _db = db;
        _logger = logger;
    }

    private static string ConnKey(int userId) => $"presence:conn:{userId}";

    public async Task<bool> SetOnlineAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Refresh persisted last-seen on every connect AND heartbeat (this runs ~every 30s while
        // online). Crucial: a session can end WITHOUT a graceful OnDisconnected — server restart,
        // crash, kill, or the Redis TTL simply expiring after the client vanished. In those cases
        // SetOfflineAsync never runs, so if last-seen were written only there it would stay stale
        // forever (a viewer sees "oxirgi marta <days ago>" for someone who was just online). By
        // touching it while online, last-seen is always at most ~one heartbeat old regardless of
        // how the session ends.
        await TouchLastSeenAsync(userId, cancellationToken);

        try
        {
            var db = _redis.GetDatabase();
            var key = ConnKey(userId);
            var existed = await db.KeyExistsAsync(key);
            var count = await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, Ttl);
            // Transition to online when the key didn't exist before (first connection / after expiry).
            return !existed || count <= 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Presence SetOnline xato (user {UserId}).", userId);
            return false;
        }
    }

    /// <summary>Persists <c>LastSeenAt = now</c> for one user via a set-based UPDATE (no entity
    /// load/tracking), best-effort. Used both while online (heartbeat) and on going offline.</summary>
    private async Task TouchLastSeenAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeenAt, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LastSeenAt yangilashda xato (user {UserId}).", userId);
        }
    }

    public async Task<bool> SetOfflineAsync(int userId, CancellationToken cancellationToken = default)
    {
        var nowOffline = false;
        try
        {
            var db = _redis.GetDatabase();
            var key = ConnKey(userId);
            var count = await db.StringDecrementAsync(key);
            if (count <= 0)
            {
                await db.KeyDeleteAsync(key);
                nowOffline = true;
            }
            else
            {
                await db.KeyExpireAsync(key, Ttl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Presence SetOffline xato (user {UserId}).", userId);
            nowOffline = true; // assume offline so last-seen still gets written
        }

        if (nowOffline)
        {
            await TouchLastSeenAsync(userId, cancellationToken);
        }

        return nowOffline;
    }

    public async Task<bool> IsOnlineAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _redis.GetDatabase().KeyExistsAsync(ConnKey(userId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Presence IsOnline xato (user {UserId}).", userId);
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<int, bool>> AreOnlineAsync(
        IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => false);
        if (ids.Count == 0) return result;

        try
        {
            var db = _redis.GetDatabase();
            var tasks = ids.ToDictionary(id => id, id => db.KeyExistsAsync(ConnKey(id)));
            await Task.WhenAll(tasks.Values);
            foreach (var (id, task) in tasks)
            {
                result[id] = task.Result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Presence AreOnline xato.");
        }

        return result;
    }
}
