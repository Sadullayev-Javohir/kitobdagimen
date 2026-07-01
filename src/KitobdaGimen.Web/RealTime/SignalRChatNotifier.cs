using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KitobdaGimen.Web.RealTime;

/// <summary>SignalR-backed <see cref="IChatNotifier"/>: pushes messages to the recipient's group.</summary>
public class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hub;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<SignalRChatNotifier> _logger;

    public SignalRChatNotifier(
        IHubContext<ChatHub> hub,
        IHubContext<NotificationHub> notificationHub,
        ILogger<SignalRChatNotifier> logger)
    {
        _hub = hub;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task MessageReceivedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(recipientUserId))
                .SendAsync("ReceiveMessage", message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Real-time delivery is best-effort; never fail the request because of it.
            _logger.LogWarning(ex, "Chat xabarini real-time yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }

    public async Task NewMessageBadgeAsync(
        int recipientUserId, int conversationId, string senderName, string? senderAvatarUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Reuse the global notification channel (connected on every page) so the navbar badge
            // updates even when /chat is closed. Shaped like a NotificationDto with type "message";
            // the client routes it to the "Xabarlar" badge instead of the activity bell.
            await _notificationHub.Clients
                .Group(NotificationHub.UserGroup(recipientUserId))
                .SendAsync("ReceiveNotification", new
                {
                    id = 0,
                    type = "message",
                    relatedId = conversationId,
                    actorName = senderName,
                    actorAvatarUrl = senderAvatarUrl,
                    message = $"{senderName} sizga xabar yubordi",
                    url = "/chat",
                    isRead = false
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Xabar bildirishnomasini yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }

    public async Task MessageEditedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(recipientUserId))
                .SendAsync("MessageEdited", message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tahrirlangan xabarni yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }

    public async Task MessageDeletedAsync(int recipientUserId, int conversationId, int messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(recipientUserId))
                .SendAsync("MessageDeleted", new { conversationId, messageId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "O'chirilgan xabarni yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }

    public async Task MessageReactionChangedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(recipientUserId))
                .SendAsync("MessageReaction", message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reaksiya o'zgarishini yuborib bo'lmadi (recipient {RecipientId}).", recipientUserId);
        }
    }

    public async Task MessagesReadAsync(int senderUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients
                .Group(ChatHub.UserGroup(senderUserId))
                .SendAsync("MessagesRead", new { conversationId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "O'qildi signalini yuborib bo'lmadi (sender {SenderId}).", senderUserId);
        }
    }

    public async Task PresenceChangedAsync(
        IEnumerable<int> recipientUserIds, int userId, bool isOnline, DateTime? lastSeenAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = recipientUserIds.Distinct().Select(ChatHub.UserGroup).ToList();
            if (groups.Count == 0) return;

            await _hub.Clients
                .Groups(groups)
                .SendAsync("PresenceChanged", new { userId, isOnline, lastSeenAt }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Presence o'zgarishini yuborib bo'lmadi (user {UserId}).", userId);
        }
    }
}
