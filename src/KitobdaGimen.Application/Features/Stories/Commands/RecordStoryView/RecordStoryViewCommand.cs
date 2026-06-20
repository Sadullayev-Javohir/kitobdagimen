using MediatR;

namespace KitobdaGimen.Application.Features.Stories.Commands.RecordStoryView;

/// <summary>
/// Records that the current user viewed a story (idempotent). The author's own views are not
/// counted. Returns the current view count.
/// </summary>
public record RecordStoryViewCommand(int StoryId) : IRequest<int>;
