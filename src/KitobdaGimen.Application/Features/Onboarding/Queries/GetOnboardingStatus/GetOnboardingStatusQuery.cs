using MediatR;

namespace KitobdaGimen.Application.Features.Onboarding.Queries.GetOnboardingStatus;

/// <summary>
/// Returns where the current user is in the signup flow so the onboarding pages
/// can only be reached as the proper next step right after registration.
/// </summary>
public record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;

public record OnboardingStatusDto
{
    /// <summary>True once the user has chosen a username (the profile step is done).</summary>
    public bool HasUsername { get; init; }

    /// <summary>True once the user has saved at least one genre (onboarding is complete).</summary>
    public bool HasGenres { get; init; }
}
