using MediatR;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;

/// <summary>Replaces the current user's selected genre interests.</summary>
public record SaveUserGenresCommand : IRequest<Unit>
{
    public IReadOnlyList<int> GenreIds { get; init; } = Array.Empty<int>();
}
