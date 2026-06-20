using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetUserStoryHistory;

/// <summary>
/// Returns ALL of a user's stories (newest first), including expired ones, for the profile page.
/// Unlike <c>GetUserStoriesQuery</c> this does not filter out expired stories — the profile shows
/// the full history with each story's time, like and view counts.
/// </summary>
public record GetUserStoryHistoryQuery(int UserId) : IRequest<IReadOnlyList<StoryDto>>;
