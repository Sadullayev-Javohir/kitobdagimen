using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A chat message inside a <see cref="Conversation"/>. May optionally share a post.
/// </summary>
public class Message : BaseEntity
{
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string? Text { get; set; }

    /// <summary>Public URL of an attached image (uploaded to <c>/uploads/chat</c>); null if none.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Key of a built-in app sticker (e.g. "book", "quill"); null if not a sticker message.</summary>
    public string? StickerKey { get; set; }

    /// <summary>Public URL of an attached voice message (uploaded to <c>/uploads/chat</c>); null if none.</summary>
    public string? VoiceUrl { get; set; }

    /// <summary>Duration of the voice message in seconds (measured on the client at record time).</summary>
    public int? VoiceDurationSeconds { get; set; }

    public int? SharedPostId { get; set; }
    public Post? SharedPost { get; set; }

    /// <summary>The message this one replies to (Telegram-style quote); null if not a reply.</summary>
    public int? ReplyToMessageId { get; set; }
    public Message? ReplyToMessage { get; set; }

    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }

    /// <summary>When the sender last edited the text; null if never edited.</summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>Emoji reactions left on this message (Telegram-style).</summary>
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}
