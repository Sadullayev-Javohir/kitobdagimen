namespace KitobdaGimen.Application.Features.Follow.Dtos;

/// <summary>A user shown in a followers/following list, with the viewer's follow state.</summary>
public record FollowUserDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
}
