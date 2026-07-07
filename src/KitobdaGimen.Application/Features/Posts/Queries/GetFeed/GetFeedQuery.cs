using KitobdaGimen.Application.Common.Models;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

/// <summary>Returns a paged, recency-ordered feed mixing posts and quotes
/// (followed users + your own; global when you follow no one).</summary>
public record GetFeedQuery : IRequest<PagedResult<FeedItemDto>>
{
    /// <summary>Free-text search across post/quote text, book title, book author and the author's name.
    /// When set, the feed searches ALL posts and quotes.</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
