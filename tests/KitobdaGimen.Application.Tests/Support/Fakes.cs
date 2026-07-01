using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Tests.Support;

/// <summary>Stub current-user service whose identity can be set per test.</summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public FakeCurrentUserService(int? userId = null, string? email = null)
    {
        UserId = userId;
        Email = email;
    }

    public int? UserId { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => UserId is not null;
}

/// <summary>Stub token service returning a fixed token; records the user it was asked about.</summary>
public class FakeTokenService : ITokenService
{
    public User? LastUser { get; private set; }

    public string GenerateToken(User user)
    {
        LastUser = user;
        return "test-token";
    }

    public TimeSpan TokenLifetime => TimeSpan.FromDays(7);
}

/// <summary>Spy chat notifier that records every push so tests can assert on real-time delivery.</summary>
public class SpyChatNotifier : IChatNotifier
{
    public List<(int RecipientUserId, MessageDto Message)> Sent { get; } = new();
    public List<(int RecipientUserId, int ConversationId, string SenderName)> MessageBadges { get; } = new();
    public List<(int RecipientUserId, MessageDto Message)> Edited { get; } = new();
    public List<(int RecipientUserId, MessageDto Message)> Reactions { get; } = new();
    public List<(int RecipientUserId, int ConversationId, int MessageId)> Deleted { get; } = new();
    public List<(int SenderUserId, int ConversationId)> ReadSignals { get; } = new();
    public List<(IReadOnlyList<int> Recipients, int UserId, bool IsOnline, DateTime? LastSeenAt)> PresenceChanges { get; } = new();

    public Task MessageReceivedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        Sent.Add((recipientUserId, message));
        return Task.CompletedTask;
    }

    public Task NewMessageBadgeAsync(
        int recipientUserId, int conversationId, string senderName, string? senderAvatarUrl,
        CancellationToken cancellationToken = default)
    {
        MessageBadges.Add((recipientUserId, conversationId, senderName));
        return Task.CompletedTask;
    }

    public Task MessageEditedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        Edited.Add((recipientUserId, message));
        return Task.CompletedTask;
    }

    public Task MessageDeletedAsync(int recipientUserId, int conversationId, int messageId, CancellationToken cancellationToken = default)
    {
        Deleted.Add((recipientUserId, conversationId, messageId));
        return Task.CompletedTask;
    }

    public Task MessageReactionChangedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default)
    {
        Reactions.Add((recipientUserId, message));
        return Task.CompletedTask;
    }

    public Task MessagesReadAsync(int senderUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        ReadSignals.Add((senderUserId, conversationId));
        return Task.CompletedTask;
    }

    public Task PresenceChangedAsync(
        IEnumerable<int> recipientUserIds, int userId, bool isOnline, DateTime? lastSeenAt,
        CancellationToken cancellationToken = default)
    {
        PresenceChanges.Add((recipientUserIds.ToList(), userId, isOnline, lastSeenAt));
        return Task.CompletedTask;
    }
}

/// <summary>Spy notification service that records every notification raised.</summary>
public class SpyNotificationService : INotificationService
{
    public List<(int RecipientUserId, NotificationDto Notification)> Sent { get; } = new();

    public Task NotifyAsync(int recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        Sent.Add((recipientUserId, notification));
        return Task.CompletedTask;
    }

    public Task NotifyManyAsync(IReadOnlyCollection<int> recipientUserIds, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        foreach (var recipientId in recipientUserIds.Distinct())
        {
            Sent.Add((recipientId, notification));
        }
        return Task.CompletedTask;
    }
}
