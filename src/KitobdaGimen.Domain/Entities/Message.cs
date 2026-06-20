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

    public int? SharedPostId { get; set; }
    public Post? SharedPost { get; set; }

    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }

    /// <summary>When the sender last edited the text; null if never edited.</summary>
    public DateTime? EditedAt { get; set; }
}
