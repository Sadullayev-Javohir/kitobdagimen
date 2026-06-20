namespace KitobdaGimen.Application.Features.Users.Dtos;

/// <summary>
/// The chat-connection relationship between the current user and another user,
/// used to decide which action button to show in search results.
/// </summary>
public enum ConnectionState
{
    /// <summary>No connection — show "Taklif qilish".</summary>
    None = 0,

    /// <summary>Current user sent a pending invite — show "Yuborildi" (cancellable).</summary>
    PendingOutgoing = 1,

    /// <summary>Current user received a pending invite — show "Qabul qilish".</summary>
    PendingIncoming = 2,

    /// <summary>Already connected — show "Suhbatlashish".</summary>
    Connected = 3
}
