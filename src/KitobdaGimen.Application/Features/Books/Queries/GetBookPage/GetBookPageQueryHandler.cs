using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBookPage;

public class GetBookPageQueryHandler : IRequestHandler<GetBookPageQuery, BookPageDto?>
{
    // Sahifa hajmi oqilona qolsin: eng yangi 100 taqriz / 200 iqtibos yetarli.
    private const int MaxPosts = 100;
    private const int MaxQuotes = 200;

    private readonly IAppDbContext _db;

    public GetBookPageQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BookPageDto?> Handle(GetBookPageQuery request, CancellationToken cancellationToken)
    {
        var book = await _db.Books
            .Where(b => b.Id == request.BookId)
            .Select(b => new
            {
                b.Id, b.Title, b.Author, b.CoverUrl, b.Source, b.TotalPages,
                GenreName = b.Genre != null ? b.Genre.Name : null,
                // Noyob kitobxonlar: taqriz yoki iqtibos yozgan foydalanuvchilar (bir kishi ko'p yozsa ham +1).
                ReaderCount = b.Posts.Select(p => p.UserId)
                    .Concat(b.Quotes.Select(q => q.UserId))
                    .Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null) return null;

        var posts = await _db.Posts
            .Where(p => p.BookId == request.BookId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(MaxPosts)
            .Select(p => new BookPagePostDto(
                p.UserId, p.User.FullName, p.User.Username, p.User.AvatarUrl,
                p.Slug, p.ReviewText, p.CreatedAt, p.Likes.Count, p.Comments.Count))
            .ToListAsync(cancellationToken);

        var quotes = await _db.Quotes
            .Where(q => q.BookId == request.BookId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(MaxQuotes)
            .Select(q => new BookPageQuoteDto(
                q.Id, q.UserId, q.User.FullName, q.User.Username, q.User.AvatarUrl,
                q.Slug, q.Text, q.CreatedAt, q.Likes.Count, q.Comments.Count))
            .ToListAsync(cancellationToken);

        return new BookPageDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            CoverUrl = book.CoverUrl,
            GenreName = book.GenreName,
            Source = book.Source,
            TotalPages = book.TotalPages,
            ReaderCount = book.ReaderCount,
            Posts = posts,
            Quotes = quotes
        };
    }
}
