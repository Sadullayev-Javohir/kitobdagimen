using MediatR;

namespace KitobdaGimen.Application.Features.Seo.Queries.GetSitemap;

/// <summary>Returns the data needed to build <c>/sitemap.xml</c>: public posts and public profiles.</summary>
public record GetSitemapQuery : IRequest<SitemapDto>;

/// <summary>All indexable URLs' raw data for the sitemap.</summary>
public record SitemapDto
{
    public IReadOnlyList<SitemapPostEntry> Posts { get; init; } = Array.Empty<SitemapPostEntry>();
    public IReadOnlyList<SitemapProfileEntry> Profiles { get; init; } = Array.Empty<SitemapProfileEntry>();
}

/// <summary>A public post: <c>/post/{AuthorRef}/{Slug}</c>.</summary>
public record SitemapPostEntry(string AuthorRef, string Slug, DateTime LastModUtc);

/// <summary>A public profile: <c>/u/{Ref}</c>.</summary>
public record SitemapProfileEntry(string Ref, DateTime LastModUtc);
