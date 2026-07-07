using MediatR;

namespace KitobdaGimen.Application.Features.Seo.Queries.GetSitemap;

/// <summary>Returns the data needed to build <c>/sitemap.xml</c>: public posts and public profiles.</summary>
public record GetSitemapQuery : IRequest<SitemapDto>;

/// <summary>All indexable URLs' raw data for the sitemap.</summary>
public record SitemapDto
{
    public IReadOnlyList<SitemapPostEntry> Posts { get; init; } = Array.Empty<SitemapPostEntry>();
    public IReadOnlyList<SitemapProfileEntry> Profiles { get; init; } = Array.Empty<SitemapProfileEntry>();
    public IReadOnlyList<SitemapQuoteEntry> Quotes { get; init; } = Array.Empty<SitemapQuoteEntry>();
    public IReadOnlyList<SitemapBookEntry> Books { get; init; } = Array.Empty<SitemapBookEntry>();
}

/// <summary>A public book page: <c>/kitob/{Id}-{slug(Title)}</c>.</summary>
public record SitemapBookEntry(int Id, string Title, DateTime LastModUtc);

/// <summary>A public post: <c>/post/{AuthorRef}/{Slug}</c>.</summary>
public record SitemapPostEntry(string AuthorRef, string Slug, DateTime LastModUtc);

/// <summary>A public profile: <c>/u/{Ref}</c>.</summary>
public record SitemapProfileEntry(string Ref, DateTime LastModUtc);

/// <summary>A public quote: <c>/iqtibos/{AuthorRef}/{Slug}</c>.</summary>
public record SitemapQuoteEntry(string AuthorRef, string Slug, DateTime LastModUtc);
