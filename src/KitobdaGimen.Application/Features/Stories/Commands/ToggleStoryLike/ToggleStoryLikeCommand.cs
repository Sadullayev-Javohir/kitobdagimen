using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Commands.ToggleStoryLike;

/// <summary>Likes or un-likes a story for the current user; returns the new like state and count.</summary>
public record ToggleStoryLikeCommand(int StoryId) : IRequest<StoryLikeResultDto>;
