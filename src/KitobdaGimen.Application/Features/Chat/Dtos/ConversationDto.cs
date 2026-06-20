using KitobdaGimen.Application.Common.Models;

namespace KitobdaGimen.Application.Features.Chat.Dtos;

/// <summary>A conversation summary for the chat list: the other participant and last-message info.</summary>
public record ConversationDto
{
    public int Id { get; init; }
    public UserSummaryDto OtherUser { get; init; } = null!;
    public string? LastMessageText { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int UnreadCount { get; init; }

    /// <summary>When the other user was last seen online (for "oxirgi marta ..." text).</summary>
    public DateTime? LastSeenAt { get; init; }

    /// <summary>Whether the other user is currently online. Filled by the Web layer from Redis presence.</summary>
    public bool IsOnline { get; set; }
}
