using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.DeleteComment;

/// <summary>Deletes a comment (and its replies) — only the comment's author may do this.</summary>
public record DeleteCommentCommand(int CommentId) : IRequest<Unit>;
