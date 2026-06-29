using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A Web Push subscription registered by a user's browser / installed app. One user can have
/// several (different devices / browsers). Used to deliver real device push notifications
/// (the TWA shows them as Android system notifications).
/// </summary>
public class PushSubscription : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>The push service endpoint URL (unique per subscription).</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>Client public key (p256dh) used to encrypt the payload.</summary>
    public string P256dh { get; set; } = null!;

    /// <summary>Client auth secret used to encrypt the payload.</summary>
    public string Auth { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
