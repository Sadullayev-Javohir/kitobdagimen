namespace KitobdaGimen.Application.Features.Chat.Dtos;

/// <summary>An emoji reaction aggregated across users for a single message.</summary>
public record MessageReactionGroupDto
{
    /// <summary>The reaction emoji.</summary>
    public string Emoji { get; init; } = "";

    /// <summary>How many users reacted with this emoji.</summary>
    public int Count { get; init; }

    /// <summary>True when the current user is one of the reactors.</summary>
    public bool Mine { get; init; }
}
