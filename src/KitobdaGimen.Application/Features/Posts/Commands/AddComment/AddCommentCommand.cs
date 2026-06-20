using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.AddComment;

/// <summary>Adds a comment (or a reply, when <see cref="ParentCommentId"/> is set) to a post.</summary>
public record AddCommentCommand : IRequest<CommentDto>
{
    public int PostId { get; init; }
    public string Text { get; init; } = null!;
    public int? ParentCommentId { get; init; }
}
