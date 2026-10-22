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

/// <summary>Stub push sender that records every web-push payload it was asked to deliver.</summary>
public class FakePushSender : IPushSender
{
    public List<(int RecipientUserId, PushNotificationPayload Payload)> Sent { get; } = new();

    public Task SendAsync(int recipientUserId, PushNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        Sent.Add((recipientUserId, payload));
        return Task.CompletedTask;
    }
}

/// <summary>Spy chat notifier that records every push so tests can assert on real-time delivery.</summary>
public class SpyChatNotifier : IChatNotifier
{
    public List<(int RecipientUserId, MessageDto Message)> Sent { get; } = new();
    public List<(int RecipientUserId, int ConversationId, string SenderName)> MessageBadges { get; } = new();
    public List<(int RecipientUserId, MessageDto Message)> Edited { get; } = new();
    public List<(int RecipientUserId, MessageDto Message)> Reactions { get; } = new();
    public List<(int RecipientUserId, string ActorName, string Emoji, int ConversationId)> ReactionNotifications { get; } = new();
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

    public Task ReactionNotificationAsync(
        int recipientUserId, string actorName, string? actorAvatarUrl, string emoji, int conversationId,
        CancellationToken cancellationToken = default)
    {
        ReactionNotifications.Add((recipientUserId, actorName, emoji, conversationId));
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

public class FakeTelegramBotService : ITelegramBotService
{
    public string BotUsername => "KitobdagiMenBot";
    public string WebAppUrl => "https://kitobdagimen.uz";

    public TelegramOtpEntry GenerateLoginOtp(int userId, long telegramId, string fullName, string? username)
        => new("123456", userId, telegramId, fullName, username, DateTime.UtcNow.AddMinutes(2));

    public TelegramOtpEntry? VerifyLoginOtp(string code) => null;
    public TelegramWebAppUser? ValidateWebAppInitData(string? initData) => null;
    public Task NotifyWebLoginSuccessAsync(long telegramId, CancellationToken ct = default) => Task.CompletedTask;
    public List<(int PostId, int LikeCount)> SyncedPostLikes { get; } = new();
    public List<(int QuoteId, int LikeCount)> SyncedQuoteLikes { get; } = new();

    public Task BroadcastPostToGroupAsync(int postId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SyncPostLikesInGroupAsync(int postId, int likeCount, CancellationToken ct = default)
    {
        SyncedPostLikes.Add((postId, likeCount));
        return Task.CompletedTask;
    }
    public Task DeletePostBroadcastAsync(int postId, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastQuoteToGroupAsync(int quoteId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SyncQuoteLikesInGroupAsync(int quoteId, int likeCount, CancellationToken ct = default)
    {
        SyncedQuoteLikes.Add((quoteId, likeCount));
        return Task.CompletedTask;
    }
    public Task DeleteQuoteBroadcastAsync(int quoteId, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastJackpotToGroupAsync(int jackpotId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SyncJackpotLikesInGroupAsync(int jackpotId, int likeCount, CancellationToken ct = default) => Task.CompletedTask;
    public Task<System.Text.Json.Nodes.JsonNode?> EditReplyMarkupAsync(long chatId, int messageId, object replyMarkup, CancellationToken ct = default) => Task.FromResult<System.Text.Json.Nodes.JsonNode?>(null);
    public Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<int> SendDailyReadingRemindersAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<bool> BroadcastMorningQuoteAsync(string text, string author, string? source, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> SendDailyGroupReportAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task BroadcastChallengeUpdateAsync(string messageHtml, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> BroadcastChallengeWinnersAsync(int year, int month, CancellationToken ct = default) => Task.FromResult(0);
    public Task<bool> IsUserInGroupAsync(long groupChatId, long telegramUserId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> IsUserMemberOfBookClubAsync(long telegramUserId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<string> GetGroupInviteLinkAsync(CancellationToken ct = default) => Task.FromResult("https://t.me/+M2zWs1tYAKoxMjli");
    public Task<System.Text.Json.Nodes.JsonNode?> SendPhotoAsync(long chatId, string photoUrlOrPath, string captionHtml, object? replyMarkup = null, int? messageThreadId = null, CancellationToken ct = default) => Task.FromResult<System.Text.Json.Nodes.JsonNode?>(null);
    public Task NotifyPhysicalBookReservedAsync(int physicalBookId, int reserverUserId, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyPhysicalBookHandoverConfirmedAsync(int physicalBookId, int ownerUserId, int reserverUserId, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyUserFollowedAsync(int followerUserId, int followingUserId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<TelegramBotUserStatsDto> GetBotUserStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new TelegramBotUserStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<BotBlockedUserItemDto>()));
    public Task<BotSyncResultDto> SyncBotBlockedStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new BotSyncResultDto(0, 0, 0, 0));
    public Task MarkUserBlockedAsync(long telegramId, CancellationToken ct = default) => Task.CompletedTask;
    public Task MarkUserUnblockedAsync(long telegramId, CancellationToken ct = default) => Task.CompletedTask;
}
