using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.DeletePost;

/// <summary>Deletes a post (and its comments/likes/views) — only the post's author may do this.</summary>
public record DeletePostCommand(int PostId) : IRequest<Unit>;
