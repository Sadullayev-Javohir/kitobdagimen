using KitobdaGimen.Application.Common.Models;
using MediatR;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBooksFeed;

/// <summary>
/// Books that already have at least one taqriz (post) or iqtibos (quote), newest activity
/// first — the listing that replaced the old <c>/quotes</c> page. Each book links to its
/// public page (<c>/kitob/{id}-{slug}</c>) where taqrizlar and iqtiboslar are shown separately.
/// </summary>
public record GetBooksFeedQuery : IRequest<PagedResult<BookFeedItemDto>>
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

/// <summary>One book card in the <c>/kitoblar</c> listing.</summary>
public record BookFeedItemDto(
    int Id, string Title, string Author, string? CoverUrl, string? Source, string? GenreName,
    int ReviewCount, int QuoteCount, DateTime LastActivityAt);
