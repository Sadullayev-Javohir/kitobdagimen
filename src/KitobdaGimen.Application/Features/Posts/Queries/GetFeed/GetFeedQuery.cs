using KitobdaGimen.Application.Common.Models;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

/// <summary>Returns a paged, recency-ordered feed mixing posts and quotes
/// (followed users + your own; global when you follow no one).</summary>
public record GetFeedQuery : IRequest<PagedResult<FeedItemDto>>
{
    /// <summary>Free-text search across post text, book title, book author and the post author's name.
    /// When set, the feed searches ALL posts (quotes are not searched) and shows posts only.</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
