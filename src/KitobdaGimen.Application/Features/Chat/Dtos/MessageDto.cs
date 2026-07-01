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
    public SharedPostPreviewDto? SharedPost { get; init; }
    public DateTime SentAt { get; init; }
    public bool IsRead { get; init; }

    /// <summary>When the message text was last edited; null if never edited.</summary>
    public DateTime? EditedAt { get; init; }

    /// <summary>True when the current user is the sender.</summary>
    public bool IsMine { get; init; }

    /// <summary>Emoji reactions on this message, grouped by emoji (Telegram-style).
    /// Populated after the base projection via <c>AttachReactionsAsync</c>.</summary>
    public List<MessageReactionGroupDto> Reactions { get; set; } = new();
}
