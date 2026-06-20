using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.UpdatePost;

/// <summary>Edits an existing post's review text and/or image — only the post's author may do this.</summary>
public record UpdatePostCommand : IRequest<PostDto>
{
    public int PostId { get; init; }
    public string ReviewText { get; init; } = null!;
    public string? ImageUrl { get; init; }
}
