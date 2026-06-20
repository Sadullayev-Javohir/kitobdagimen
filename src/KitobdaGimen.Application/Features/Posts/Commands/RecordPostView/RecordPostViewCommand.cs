using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.RecordPostView;

/// <summary>Records that the current user viewed a post. Idempotent (one view per user per post).</summary>
public record RecordPostViewCommand(int PostId) : IRequest<Unit>;
