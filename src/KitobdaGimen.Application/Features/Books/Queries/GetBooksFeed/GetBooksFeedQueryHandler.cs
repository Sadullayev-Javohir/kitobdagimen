using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBooksFeed;

public class GetBooksFeedQueryHandler : IRequestHandler<GetBooksFeedQuery, PagedResult<BookFeedItemDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;

    public GetBooksFeedQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<BookFeedItemDto>> Handle(GetBooksFeedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var isSearch = !string.IsNullOrWhiteSpace(request.Search);
        var query = _db.Books.AsQueryable();

        if (isSearch)
        {
            query = query.WhereMatchesBookSearch(request.Search);
        }
        else
        {
            // Bosh sahifada (qidiruvsiz) faqat faolligi (taqriz yoki iqtibos) bor kitoblar chiqadi.
            query = query.Where(b => b.Posts.Any() || b.Quotes.Any());
        }

        var projected = query.Select(b => new
        {
            b.Id,
            b.Title,
            b.Author,
            b.CoverUrl,
            b.Source,
            GenreName = b.Genre != null ? b.Genre.Name : null,
            ReviewCount = b.Posts.Count,
            QuoteCount = b.Quotes.Count,
            // Noyob kitobxonlar: taqriz yoki iqtibos yozgan foydalanuvchilar (bir kishi ko'p yozsa ham +1).
            ReaderCount = b.Posts.Select(p => p.UserId)
                .Concat(b.Quotes.Select(q => q.UserId))
                .Distinct().Count(),
            LastPost = b.Posts.Max(p => (DateTime?)p.CreatedAt),
            LastQuote = b.Quotes.Max(q => (DateTime?)q.CreatedAt)
        });

        var totalCount = await projected.CountAsync(cancellationToken);

        // Qidiruvda avval eng ko'p o'qilgan/muhokama qilingan kitoblar, keyin nomi bo'yicha.
        // Qidiruvsiz esa eng so'nggi faollik (taqriz/iqtibos) vaqti bo'yicha tartiblanadi.
        var ordered = isSearch
            ? projected
                .OrderByDescending(b => b.ReviewCount + b.QuoteCount)
                .ThenBy(b => b.Title)
            : projected
                .OrderByDescending(b => b.LastPost == null ? b.LastQuote
                    : b.LastQuote == null ? b.LastPost
                    : (b.LastPost > b.LastQuote ? b.LastPost : b.LastQuote));

        var raw = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = raw
            .Select(b => new BookFeedItemDto(
                b.Id, b.Title, b.Author, b.CoverUrl, b.Source, b.GenreName,
                b.ReviewCount, b.QuoteCount, b.ReaderCount,
                (b.LastPost == null ? b.LastQuote
                    : b.LastQuote == null ? b.LastPost
                    : (b.LastPost > b.LastQuote ? b.LastPost : b.LastQuote)) ?? DateTime.UtcNow))
            .ToList();

        return PagedResult<BookFeedItemDto>.Create(items, page, pageSize, totalCount);
    }
}
