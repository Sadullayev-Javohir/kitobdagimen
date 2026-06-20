namespace KitobdaGimen.Application.Features.Stories.Dtos;

/// <summary>Result of toggling a like on a story.</summary>
public record StoryLikeResultDto
{
    public bool IsLiked { get; init; }
    public int LikeCount { get; init; }
}
