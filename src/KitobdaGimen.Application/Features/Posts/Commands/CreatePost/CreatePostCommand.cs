using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Commands.CreatePost;

/// <summary>Creates a new post about a book for the current user.</summary>
public record CreatePostCommand : IRequest<PostDto>
{
    public int BookId { get; init; }
    public string ReviewText { get; init; } = null!;
    public string? ImageUrl { get; init; }
}
