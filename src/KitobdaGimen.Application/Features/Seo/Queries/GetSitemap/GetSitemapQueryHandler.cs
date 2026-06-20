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

        return new SitemapDto { Posts = posts, Profiles = profiles };
    }
}
