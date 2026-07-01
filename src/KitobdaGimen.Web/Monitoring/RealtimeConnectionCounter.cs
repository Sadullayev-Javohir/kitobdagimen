namespace KitobdaGimen.Web.Monitoring;

/// <summary>
/// Process-wide count of live SignalR connections (chat + notifications), maintained with
/// interlocked increments from the hubs' OnConnected/OnDisconnected. Singleton, lock-free.
/// This is an aggregate number only — no user identities are stored.
/// </summary>
public sealed class RealtimeConnectionCounter
{
    private long _active;

    /// <summary>Current number of open hub connections (never negative).</summary>
    public int Active => (int)Math.Max(0, Interlocked.Read(ref _active));

    public void Increment() => Interlocked.Increment(ref _active);

    public void Decrement()
    {
        // Guard against underflow if a disconnect is ever seen without a matching connect.
        if (Interlocked.Decrement(ref _active) < 0)
        {
            Interlocked.Exchange(ref _active, 0);
        }
    }
}
