namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Tracks which users are currently online. Implemented in the Web/Infrastructure layer with
/// Redis (TTL-based heartbeat). Online state is ephemeral; "last seen" is persisted on the user.
/// </summary>
public interface IPresenceService
{
    /// <summary>Marks a user online / refreshes their heartbeat TTL.
    /// Returns true if this made them newly online (a transition worth broadcasting).</summary>
    Task<bool> SetOnlineAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Marks one connection offline and, when the last one closes, records last-seen.
    /// Returns true if the user is now fully offline (a transition worth broadcasting).</summary>
    Task<bool> SetOfflineAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> IsOnlineAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Bulk online check; returns a map of userId → isOnline for the given ids.</summary>
    Task<IReadOnlyDictionary<int, bool>> AreOnlineAsync(
        IEnumerable<int> userIds, CancellationToken cancellationToken = default);
}
