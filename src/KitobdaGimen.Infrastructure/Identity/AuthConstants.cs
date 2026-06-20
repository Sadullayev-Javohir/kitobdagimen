namespace KitobdaGimen.Infrastructure.Identity;

/// <summary>
/// Shared authentication constants. The app issues its own JWT after Google login and
/// stores it in an HttpOnly cookie; the JWT bearer handler reads the token from that cookie.
/// </summary>
public static class AuthConstants
{
    /// <summary>Name of the HttpOnly cookie that carries the issued JWT.</summary>
    public const string AccessTokenCookie = "kitobdagimen_token";

    /// <summary>
    /// Temporary cookie scheme used only to correlate the external Google OAuth round-trip.
    /// </summary>
    public const string ExternalScheme = "External";
}
