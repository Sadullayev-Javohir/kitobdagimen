namespace KitobdaGimen.Application.Features.Quotes.Dtos;

/// <summary>
/// A user who liked a quote, including follow state and active story indicator.
/// </summary>
public record QuoteLikerDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
    public bool IsCurrentUser { get; init; }
    public bool HasActiveStory { get; init; }
    public DateTime LikedAt { get; init; }
}
