namespace KitobdaGimen.Application.Features.Posts.Dtos;

/// <summary>Result of toggling a like: the new state and the updated total.</summary>
public record LikeResultDto
{
    public bool IsLiked { get; init; }
    public int LikeCount { get; init; }
}
