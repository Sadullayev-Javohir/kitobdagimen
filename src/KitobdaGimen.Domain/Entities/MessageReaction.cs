using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A single emoji reaction a user left on a <see cref="Message"/> (Telegram-style).
/// A user may hold at most one reaction per message (enforced by a unique index).
/// </summary>
public class MessageReaction : BaseEntity
{
    public int MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>The reaction emoji, e.g. "❤️", "👍", "😂".</summary>
    public string Emoji { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
