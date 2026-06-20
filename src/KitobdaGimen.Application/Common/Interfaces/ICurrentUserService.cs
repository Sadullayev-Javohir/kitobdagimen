namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user, resolved from the request context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's database id, or <c>null</c> if anonymous.</summary>
    int? UserId { get; }

    /// <summary>The authenticated user's email, or <c>null</c> if anonymous.</summary>
    string? Email { get; }

    bool IsAuthenticated { get; }
}
