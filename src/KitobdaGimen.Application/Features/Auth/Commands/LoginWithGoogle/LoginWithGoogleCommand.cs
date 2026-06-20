using KitobdaGimen.Application.Features.Auth.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;

/// <summary>
/// Signs a user in from the Google OAuth profile, creating the account on first login.
/// </summary>
public record LoginWithGoogleCommand : IRequest<AuthResultDto>
{
    public string GoogleId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}
