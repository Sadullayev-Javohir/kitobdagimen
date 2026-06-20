using KitobdaGimen.Application.Features.Auth.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.CompleteProfile;

/// <summary>Sets the mandatory username and full name after a user's first Google signup.</summary>
public record CompleteProfileCommand : IRequest<UserDto>
{
    public string Username { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}
