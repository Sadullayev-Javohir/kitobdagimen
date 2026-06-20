using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Commands.DeleteStory;

/// <summary>Deletes a story (only its author may do this).</summary>
public record DeleteStoryCommand(int StoryId) : IRequest<Unit>;
