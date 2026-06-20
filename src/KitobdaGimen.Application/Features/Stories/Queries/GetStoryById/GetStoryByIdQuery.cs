using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetStoryById;

/// <summary>
/// Returns a single story (by id) for its detail page, or throws if not found.
/// Includes expired stories — the detail page shows the full story regardless of duration.
/// </summary>
public record GetStoryByIdQuery(int StoryId) : IRequest<StoryDto>;
