using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Seo.Queries.GetSitemap;

public class GetSitemapQueryHandler : IRequestHandler<GetSitemapQuery, SitemapDto>
{
    // Sitemap protokoli bitta faylda 50 000 URL bilan cheklaydi; xavfsiz chegara.
    private const int MaxUrls = 20000;

    private readonly IAppDbContext _db;

    public GetSitemapQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SitemapDto> Handle(GetSitemapQuery request, CancellationToken cancellationToken)
    {
        var rawPosts = await _db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Take(MaxUrls)
            .Select(p => new { p.User.Username, p.UserId, p.Slug, p.CreatedAt })
            .ToListAsync(cancellationToken);

        var posts = rawPosts
            .Select(p => new SitemapPostEntry(
                string.IsNullOrWhiteSpace(p.Username) ? p.UserId.ToString() : p.Username!,
                p.Slug,
                p.CreatedAt))
            .ToList();

        // Faqat username'i bor va kamida bitta posti bor foydalanuvchilar (bo'sh profil indeksga arzimaydi).
        var rawProfiles = await _db.Users
            .Where(u => u.Username != null && u.Posts.Any())
            .Select(u => new { u.Username, LastPost = u.Posts.Max(p => p.CreatedAt) })
            .ToListAsync(cancellationToken);

        var profiles = rawProfiles
            .Select(u => new SitemapProfileEntry(u.Username!, u.LastPost))
            .ToList();

        // Ommaviy iqtiboslar (kanonik /iqtibos/{username}/{slug}) — postlar kabi indekslanadi.
        var rawQuotes = await _db.Quotes
            .OrderByDescending(q => q.CreatedAt)
            .Take(MaxUrls)
            .Select(q => new { q.User.Username, q.UserId, q.Slug, q.CreatedAt })
            .ToListAsync(cancellationToken);

        var quotes = rawQuotes
            .Select(q => new SitemapQuoteEntry(
                string.IsNullOrWhiteSpace(q.Username) ? q.UserId.ToString() : q.Username!,
                q.Slug,
                q.CreatedAt))
            .ToList();

        // Kitob sahifalari (/kitob/{id}-{nom}) — faqat kontenti (taqriz yoki iqtibos) borlari;
        // lastmod = oxirgi taqriz/iqtibos sanasi.
        var rawBooks = await _db.Books
            .Where(b => b.Posts.Any() || b.Quotes.Any())
            .Select(b => new
            {
                b.Id, b.Title,
                LastPost = b.Posts.Max(p => (DateTime?)p.CreatedAt),
                LastQuote = b.Quotes.Max(q => (DateTime?)q.CreatedAt)
            })
            .Take(MaxUrls)
            .ToListAsync(cancellationToken);

        var books = rawBooks
            .Select(b => new SitemapBookEntry(
                b.Id, b.Title,
                new[] { b.LastPost, b.LastQuote }.Max() ?? DateTime.UtcNow))
            .ToList();

        return new SitemapDto { Posts = posts, Profiles = profiles, Quotes = quotes, Books = books };
    }
}
