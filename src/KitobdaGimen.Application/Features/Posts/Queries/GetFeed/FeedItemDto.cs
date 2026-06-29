using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Quotes.Dtos;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetFeed;

/// <summary>
/// One item in the unified feed — either a post (book review) or a quote. The feed mixes both
/// kinds in a single, recency-ordered stream so quotes appear alongside posts.
/// </summary>
public record FeedItemDto
{
    /// <summary>"post" or "quote".</summary>
    public string Kind { get; init; } = "post";

    /// <summary>Creation time, used to order the mixed stream.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Set when <see cref="Kind"/> is "post".</summary>
    public PostDto? Post { get; init; }

    /// <summary>Set when <see cref="Kind"/> is "quote".</summary>
    public QuoteDto? Quote { get; init; }

    public static FeedItemDto FromPost(PostDto post) => new()
    {
        Kind = "post",
        CreatedAt = post.CreatedAt,
        Post = post
    };

    public static FeedItemDto FromQuote(QuoteDto quote) => new()
    {
        Kind = "quote",
        CreatedAt = quote.CreatedAt,
        Quote = quote
    };
}
