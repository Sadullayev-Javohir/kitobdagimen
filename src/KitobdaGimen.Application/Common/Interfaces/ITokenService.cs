using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Issues JWT access tokens for authenticated users. The token is stored in an
/// HttpOnly cookie by the Web layer.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a signed JWT for the given user.</summary>
    string GenerateToken(User user);

    /// <summary>Lifetime of issued tokens — used by the Web layer to set the cookie expiry.</summary>
    TimeSpan TokenLifetime { get; }
}
