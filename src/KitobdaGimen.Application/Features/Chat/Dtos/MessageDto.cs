using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Chat.Dtos;

/// <summary>A chat message, with the sender, optional shared post and read state.</summary>
public record MessageDto
{
    public int Id { get; init; }
    public int ConversationId { get; init; }
    public UserSummaryDto Sender { get; init; } = null!;
    public string? Text { get; init; }
    public string? ImageUrl { get; init; }
    public string? StickerKey { get; init; }

    /// <summary>Public URL of an attached voice message; null if not a voice message.</summary>
    public string? VoiceUrl { get; init; }

    /// <summary>Voice message duration in seconds (for the player UI); null if not a voice message.</summary>
    public int? VoiceDurationSeconds { get; init; }

    public SharedPostPreviewDto? SharedPost { get; init; }
    public DateTime SentAt { get; init; }
    public bool IsRead { get; init; }

    /// <summary>Id of the quoted message this one replies to; null if not a reply.</summary>
    public int? ReplyToId { get; init; }

    /// <summary>Short preview text of the quoted message (its text, or "Rasm"/"Ovozli xabar"/…).</summary>
    public string? ReplyToPreview { get; init; }

    /// <summary>Display name of the quoted message's sender.</summary>
    public string? ReplyToSenderName { get; init; }

    /// <summary>When the message text was last edited; null if never edited.</summary>
    public DateTime? EditedAt { get; init; }

    /// <summary>True when the current user is the sender.</summary>
    public bool IsMine { get; init; }

    /// <summary>Emoji reactions on this message, grouped by emoji (Telegram-style).
    /// Populated after the base projection via <c>AttachReactionsAsync</c>.</summary>
    public List<MessageReactionGroupDto> Reactions { get; set; } = new();
}
