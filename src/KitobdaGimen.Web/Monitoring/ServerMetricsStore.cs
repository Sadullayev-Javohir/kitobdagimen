using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Admin.Monitoring;

namespace KitobdaGimen.Web.Monitoring;

/// <summary>
/// In-memory bounded ring buffer of recent <see cref="ServerSnapshot"/>s (singleton). The
/// collector writes the newest snapshot; the admin query reads <see cref="Latest"/> and
/// <see cref="History"/>. Thread-safe via a simple lock — writes are infrequent (every few
/// seconds) and reads are cheap, so contention is negligible.
/// </summary>
public sealed class ServerMetricsStore : IServerMetricsStore
{
    private readonly object _gate = new();
    private readonly int _capacity;
    private readonly Queue<ServerSnapshot> _buffer;
    private ServerSnapshot? _latest;

    public ServerMetricsStore(int capacity = 150)
    {
        _capacity = Math.Max(1, capacity);
        _buffer = new Queue<ServerSnapshot>(_capacity);
    }

    public void Add(ServerSnapshot snapshot)
    {
        if (snapshot is null) return;

        lock (_gate)
        {
            _buffer.Enqueue(snapshot);
            while (_buffer.Count > _capacity)
            {
                _buffer.Dequeue();
            }
            _latest = snapshot;
        }
    }

    public ServerSnapshot? Latest
    {
        get
        {
            lock (_gate)
            {
                return _latest;
            }
        }
    }

    public IReadOnlyList<ServerSnapshot> History
    {
        get
        {
            lock (_gate)
            {
                return _buffer.ToArray();
            }
        }
    }
}
