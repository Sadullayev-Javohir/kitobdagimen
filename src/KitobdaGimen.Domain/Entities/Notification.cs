using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A persisted activity notification (like, comment, follow, chat invite, invite accepted).
/// Persisting means a notification raised while the recipient is offline is not lost — it is
/// replayed when the recipient next loads a page (badge count) and is surfaced from the DB,
/// not only pushed transiently over SignalR.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>The user who should see this notification.</summary>
    public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    /// <summary>Kind: "like", "comment", "follow", "connection_request" or "connection_accepted".</summary>
    public string Type { get; set; } = "";

    /// <summary>Optional related entity id (e.g. the connection id for an invite).</summary>
    public int? RelatedId { get; set; }

    /// <summary>The user who triggered the notification. Plain id (no FK) — denormalised name/avatar
    /// below keep the notification readable even if the actor later deletes their account.</summary>
    public int? ActorId { get; set; }

    public string ActorName { get; set; } = "";
    public string? ActorAvatarUrl { get; set; }

    /// <summary>Optional bold heading shown above the message. Used by super-admin broadcasts
    /// ("announcement") which carry a title + body; null for ordinary activity notifications.</summary>
    public string? Title { get; set; }

    /// <summary>Human-readable message in Uzbek.</summary>
    public string Message { get; set; } = "";

    /// <summary>Optional link the notification points to.</summary>
    public string? Url { get; set; }

    /// <summary>False until the recipient has seen it (opening /chat marks invites read).</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
