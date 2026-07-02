using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KitobdaGimen.Web.RealTime;

/// <summary>
/// <see cref="INotificationService"/> that first PERSISTS each notification, then pushes it over
/// SignalR. Persisting is what makes an invite (or any notification) raised while the recipient is
/// offline survive: the SignalR push to an empty group is lost, but the row remains and is replayed
/// when the recipient next loads a page (unread-count badge) or opens /chat.
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IAppDbContext _db;
    private readonly IPushSender _push;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hub, IAppDbContext db, IPushSender push, ILogger<SignalRNotificationService> logger)
    {
        _hub = hub;
        _db = db;
        _push = push;
        _logger = logger;
    }

    public async Task NotifyAsync(int recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        // 1) Persist so the notification is never lost while the recipient is offline.
        var entity = new Notification
        {
            RecipientId = recipientUserId,
            Type = notification.Type,
            RelatedId = notification.RelatedId,
            ActorId = notification.ActorId,
            ActorName = notification.ActorName,
            ActorAvatarUrl = notification.ActorAvatarUrl,
            Title = notification.Title,
            Message = notification.Message,
            Url = notification.Url,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // 2) Push live (harmless no-op if the recipient has no open clients).
        await PushAsync(recipientUserId, notification with { Id = entity.Id, CreatedAt = entity.CreatedAt, IsRead = false }, cancellationToken);
    }

    public async Task NotifyManyAsync(IReadOnlyCollection<int> recipientUserIds, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        // De-dupe so a recipient never gets the same notification twice.
        var recipients = recipientUserIds.Distinct().ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        // 1) Persist one row per recipient in a single round-trip.
        var createdAt = DateTime.UtcNow;
        var entities = recipients.Select(recipientId => new Notification
        {
            RecipientId = recipientId,
            Type = notification.Type,
            RelatedId = notification.RelatedId,
            ActorId = notification.ActorId,
            ActorName = notification.ActorName,
            ActorAvatarUrl = notification.ActorAvatarUrl,
            Title = notification.Title,
            Message = notification.Message,
            Url = notification.Url,
            IsRead = false,
            CreatedAt = createdAt
        }).ToList();
        _db.Notifications.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);

        // 2) Push each live with its own persisted id.
        foreach (var entity in entities)
        {
            await PushAsync(entity.RecipientId, notification with { Id = entity.Id, CreatedAt = entity.CreatedAt, IsRead = false }, cancellationToken);
        }
    }

    private async Task PushAsync(int recipientUserId, NotificationDto payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients
                .Group(NotificationHub.UserGroup(recipientUserId))
                .SendAsync("ReceiveNotification", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bildirishnomani real-time yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }

        // Real qurilma push (TWA -> Android bildirishnoma tovoqchasi). Best-effort.
        try
        {
            await _push.SendAsync(recipientUserId, new PushNotificationPayload
            {
                Title = !string.IsNullOrWhiteSpace(payload.Title) ? payload.Title
                    : string.IsNullOrWhiteSpace(payload.ActorName) ? "kitobdagimen.uz" : payload.ActorName,
                Body = payload.Message,
                Url = payload.Url,
                Icon = string.IsNullOrWhiteSpace(payload.ActorAvatarUrl) ? "/img/icons/icon-192.png" : payload.ActorAvatarUrl,
                Tag = payload.Type
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Qurilma push yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }
}
