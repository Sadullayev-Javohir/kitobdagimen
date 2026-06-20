using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

/// <summary>Returns a paged feed of posts (followed users first, falling back to recent posts).</summary>
public record GetFeedQuery : IRequest<PagedResult<PostDto>>
{
    /// <summary>Free-text search across post text, book title, book author and the post author's name.
    /// When set, the feed searches ALL posts (not just followed authors).</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
