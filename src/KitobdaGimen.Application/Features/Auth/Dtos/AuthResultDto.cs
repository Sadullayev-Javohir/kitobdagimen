namespace KitobdaGimen.Application.Features.Auth.Dtos;

/// <summary>
/// Result of a successful login. The Web layer writes <see cref="Token"/> into the HttpOnly cookie
/// and redirects to onboarding when <see cref="RequiresOnboarding"/> is true.
/// </summary>
public record AuthResultDto
{
    public string Token { get; init; } = null!;
    public UserDto User { get; init; } = null!;

    /// <summary>True when the user has not yet selected their genre interests.</summary>
    public bool RequiresOnboarding { get; init; }

    /// <summary>True when the user has not yet set a username and full name (mandatory after first signup).</summary>
    public bool RequiresProfileSetup { get; init; }
}
