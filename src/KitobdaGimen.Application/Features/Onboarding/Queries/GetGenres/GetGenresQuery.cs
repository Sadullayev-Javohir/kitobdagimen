using KitobdaGimen.Application.Features.Onboarding.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Onboarding.Queries.GetGenres;

/// <summary>Returns all available genres for the onboarding interest picker.</summary>
public record GetGenresQuery : IRequest<IReadOnlyList<GenreDto>>;
