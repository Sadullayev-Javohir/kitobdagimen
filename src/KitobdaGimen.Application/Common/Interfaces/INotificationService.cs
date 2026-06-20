using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Pushes activity notifications (likes, comments, follows) to a user in real time.
/// Implemented in the Web layer with SignalR.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(int recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fans the same notification out to many recipients (e.g. an author's followers when they post a
    /// review or a quote), persisting all rows in a single round-trip before pushing each one live.
    /// </summary>
    Task NotifyManyAsync(IReadOnlyCollection<int> recipientUserIds, NotificationDto notification, CancellationToken cancellationToken = default);
}
