using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Queries.CheckUsername;

/// <summary>
/// Checks whether <paramref name="Username"/> is a valid, still-free username for the current user
/// (their own current username always counts as available). Used by the profile-edit live check.
/// </summary>
public record CheckUsernameQuery(string? Username) : IRequest<UsernameCheckDto>;

/// <summary>Result of a live username availability check.</summary>
public record UsernameCheckDto
{
    /// <summary>True when the username matches the format rules.</summary>
    public bool IsValid { get; init; }

    /// <summary>True when the username is free (or already belongs to the current user).</summary>
    public bool IsAvailable { get; init; }

    /// <summary>Human-readable status message (Uzbek).</summary>
    public string Message { get; init; } = null!;
}
