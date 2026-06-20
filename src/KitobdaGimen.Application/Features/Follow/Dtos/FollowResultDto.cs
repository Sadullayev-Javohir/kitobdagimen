namespace KitobdaGimen.Application.Features.Follow.Dtos;

/// <summary>Result of toggling a follow: the new state and the target's updated follower count.</summary>
public record FollowResultDto
{
    public bool IsFollowing { get; init; }
    public int FollowerCount { get; init; }
}
