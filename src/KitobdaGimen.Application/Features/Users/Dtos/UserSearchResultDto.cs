namespace KitobdaGimen.Application.Features.Users.Dtos;

/// <summary>A user shown in the /chat people search, with relationship and presence info.</summary>
public record UserSearchResultDto
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }

    /// <summary>True when the user has at least one active (non-expired) story.</summary>
    public bool HasStory { get; init; }

    /// <summary>Relationship state, drives the action button.</summary>
    public ConnectionState ConnectionState { get; init; }

    /// <summary>The connection row id (when one exists) — needed to accept/cancel inline.</summary>
    public int? ConnectionId { get; init; }

    /// <summary>Whether the user is currently online. Filled by the Web layer from Redis presence
    /// (the Application layer leaves it false).</summary>
    public bool IsOnline { get; set; }

    public DateTime? LastSeenAt { get; init; }
}
