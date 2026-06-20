using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetUserStories;

/// <summary>Returns a user's stories (oldest first) for the story viewer.</summary>
public record GetUserStoriesQuery(int UserId) : IRequest<IReadOnlyList<StoryDto>>;
