using KitobdaGimen.Application.Features.Chat.Dtos;

namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Pushes chat messages to recipients in real time. Implemented in the Web layer with SignalR.
/// </summary>
public interface IChatNotifier
{
    /// <summary>Delivers a newly sent message to the recipient's open chat clients.</summary>
    Task MessageReceivedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals the recipient (on ANY page, not just /chat) that a new message arrived, so the navbar
    /// "Xabarlar" badge lights up in real time. Pushed over the global notification hub.
    /// </summary>
    Task NewMessageBadgeAsync(
        int recipientUserId, int conversationId, string senderName, string? senderAvatarUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Tells the recipient that an existing message was edited (so they can update its text live).</summary>
    Task MessageEditedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default);

    /// <summary>Tells the recipient that a message was deleted (so they can remove it live).</summary>
    Task MessageDeletedAsync(int recipientUserId, int conversationId, int messageId, CancellationToken cancellationToken = default);

    /// <summary>Tells the recipient that a message's emoji reactions changed (so they update live).</summary>
    Task MessageReactionChangedAsync(int recipientUserId, MessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells the original sender that their messages in a conversation were read, so their
    /// outgoing messages can switch to the blue double-tick in real time.
    /// </summary>
    Task MessagesReadAsync(int senderUserId, int conversationId, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a user's online/offline change to the given recipients (their connections).</summary>
    Task PresenceChangedAsync(
        IEnumerable<int> recipientUserIds, int userId, bool isOnline, DateTime? lastSeenAt,
        CancellationToken cancellationToken = default);
}
