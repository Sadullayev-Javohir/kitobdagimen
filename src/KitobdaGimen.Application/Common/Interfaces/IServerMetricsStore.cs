using KitobdaGimen.Application.Features.Admin.Monitoring;

namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// In-memory store of recent <see cref="ServerSnapshot"/>s. Implemented in the Web layer as a
/// bounded ring buffer (singleton). The collector writes; the hub and the admin query read.
/// </summary>
public interface IServerMetricsStore
{
    /// <summary>Adds the newest snapshot, evicting the oldest when the buffer is full.</summary>
    void Add(ServerSnapshot snapshot);

    /// <summary>The most recent snapshot, or <c>null</c> if none collected yet.</summary>
    ServerSnapshot? Latest { get; }

    /// <summary>Recent snapshots oldest-first, for sparkline charts (bounded).</summary>
    IReadOnlyList<ServerSnapshot> History { get; }
}
