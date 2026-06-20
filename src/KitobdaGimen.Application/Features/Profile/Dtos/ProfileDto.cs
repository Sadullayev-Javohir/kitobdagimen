namespace KitobdaGimen.Application.Features.Profile.Dtos;

/// <summary>A user's public profile with aggregate counts and the viewer's follow state.</summary>
public record ProfileDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }

    public int PostCount { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }

    /// <summary>True when the current user follows this profile.</summary>
    public bool IsFollowedByCurrentUser { get; init; }

    /// <summary>True when this profile belongs to the current user.</summary>
    public bool IsCurrentUser { get; init; }

    /// <summary>True when this user has at least one story.</summary>
    public bool HasStory { get; init; }
}
