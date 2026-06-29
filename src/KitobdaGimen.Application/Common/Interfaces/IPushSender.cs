namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>The content of a device push notification.</summary>
public record PushNotificationPayload
{
    public string Title { get; init; } = "kitobdagimen.uz";
    public string Body { get; init; } = "";
    /// <summary>App path to open when the notification is tapped (e.g. "/feed").</summary>
    public string? Url { get; init; }
    /// <summary>Notification icon URL.</summary>
    public string? Icon { get; init; }
    /// <summary>Grouping tag so similar notifications collapse.</summary>
    public string? Tag { get; init; }
}

/// <summary>
/// Sends Web Push messages to a user's registered devices. The TWA delegates these to the
/// Android system notification tray (Telegram-style). Implemented in Infrastructure with VAPID.
/// Best-effort: failures are swallowed/logged and expired subscriptions are pruned.
/// </summary>
public interface IPushSender
{
    Task SendAsync(int recipientUserId, PushNotificationPayload payload, CancellationToken cancellationToken = default);
}
