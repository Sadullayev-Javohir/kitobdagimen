using KitobdaGimen.Application.Features.Profile.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Commands.UpdateProfile;

/// <summary>Updates the current user's editable profile fields.</summary>
public record UpdateProfileCommand : IRequest<ProfileDto>
{
    public string Username { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
}
