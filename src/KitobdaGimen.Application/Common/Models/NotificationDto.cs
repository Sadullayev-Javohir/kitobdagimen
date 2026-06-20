namespace KitobdaGimen.Application.Common.Models;

/// <summary>A real-time activity notification shown to the recipient.</summary>
public record NotificationDto
{
    /// <summary>Persisted notification id (0 until saved). Lets the client mark it read.</summary>
    public int Id { get; init; }

    /// <summary>Notification kind: "like", "comment", "follow", "connection_request" or "connection_accepted".</summary>
    public string Type { get; init; } = "";

    /// <summary>Optional related entity id (e.g. the connection id for invite notifications),
    /// so the client can act on it inline (accept/decline).</summary>
    public int? RelatedId { get; init; }

    /// <summary>Optional id of the actor (the user who triggered the notification).</summary>
    public int? ActorId { get; init; }

    /// <summary>Human-readable message in Uzbek.</summary>
    public string Message { get; init; } = "";

    /// <summary>Optional link the notification points to (e.g. a post).</summary>
    public string? Url { get; init; }

    /// <summary>Display name of the user who triggered the notification.</summary>
    public string ActorName { get; init; } = "";

    public string? ActorAvatarUrl { get; init; }

    /// <summary>Whether the recipient has already seen this notification.</summary>
    public bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
