using System.Net;
using System.Text.RegularExpressions;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.YearReview.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.YearReview.Queries.GetYearReview;

public class GetYearReviewQueryHandler : IRequestHandler<GetYearReviewQuery, YearReviewDto>
{
    /// <summary>Kartochkada ko'rsatiladigan kitoblarning maksimal soni.</summary>
    private const int MaxBooks = 8;

    private const int PostSnippetLength = 140;
    private const int QuoteSnippetLength = 160;

    private readonly IAppDbContext _db;

    public GetYearReviewQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<YearReviewDto> Handle(GetYearReviewQuery request, CancellationToken ct)
    {
        var uid = request.UserId;
        var year = request.Year;
        var (from, to) = YearReviewCalendar.YearRange(year);

        // ── O'qish statistikasi (ReadingProgress) ──────────────────────────────────
        var rows = await _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0
                        && p.ReadingGoal.UserId == uid
                        && p.Date >= from && p.Date <= to)
            .Select(p => new { p.Date, p.PagesReadToday, p.ReadingGoal.BookId })
            .ToListAsync(ct);

        var totalPages = rows.Sum(r => r.PagesReadToday);
        var activeDays = rows.Select(r => r.Date).Distinct().Count();

        // Kitob bo'yicha o'qilgan betlar (eng ko'pi birinchi).
        var pagesByBook = rows
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Pages = g.Sum(r => r.PagesReadToday) })
            .OrderByDescending(x => x.Pages)
            .ToList();

        var booksRead = pagesByBook.Count;

        var topBookIds = pagesByBook.Take(MaxBooks).Select(x => x.BookId).ToList();
        var bookInfos = await _db.Books
            .Where(b => topBookIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Title, b.Author, b.CoverUrl })
            .ToListAsync(ct);

        var books = pagesByBook
            .Take(MaxBooks)
            .Select(x =>
            {
                var info = bookInfos.FirstOrDefault(b => b.Id == x.BookId);
                return new YearReviewBookDto
                {
                    Title = info?.Title ?? "Noma'lum kitob",
                    Author = info?.Author ?? "",
                    CoverUrl = info?.CoverUrl,
                    Pages = x.Pages
                };
            })
            .ToList();

        // ── Eng ko'p like yig'gan post (shu yil ichida yaratilgan) ──────────────────
        var topPostRaw = await _db.Posts
            .Where(p => p.UserId == uid && p.CreatedAt.Year == year)
            .Select(p => new
            {
                p.Slug,
                Username = p.User.Username,
                p.ReviewText,
                BookTitle = p.Book.Title,
                LikeCount = p.Likes.Count
            })
            .OrderByDescending(p => p.LikeCount)
            .ThenByDescending(p => p.Slug)
            .FirstOrDefaultAsync(ct);

        YearReviewTopPostDto? topPost = null;
        if (topPostRaw is not null && topPostRaw.LikeCount > 0)
        {
            topPost = new YearReviewTopPostDto
            {
                Slug = topPostRaw.Slug,
                Username = topPostRaw.Username,
                Snippet = Snippet(StripHtml(topPostRaw.ReviewText), PostSnippetLength),
                BookTitle = topPostRaw.BookTitle,
                LikeCount = topPostRaw.LikeCount
            };
        }

        // ── Eng ko'p like yig'gan iqtibos (shu yil ichida yaratilgan) ───────────────
        var topQuoteRaw = await _db.Quotes
            .Where(q => q.UserId == uid && q.CreatedAt.Year == year)
            .Select(q => new
            {
                q.Id,
                q.Text,
                BookTitle = q.Book.Title,
                LikeCount = q.Likes.Count
            })
            .OrderByDescending(q => q.LikeCount)
            .ThenByDescending(q => q.Id)
            .FirstOrDefaultAsync(ct);

        YearReviewTopQuoteDto? topQuote = null;
        if (topQuoteRaw is not null && topQuoteRaw.LikeCount > 0)
        {
            topQuote = new YearReviewTopQuoteDto
            {
                Id = topQuoteRaw.Id,
                Snippet = Snippet(topQuoteRaw.Text, QuoteSnippetLength),
                BookTitle = topQuoteRaw.BookTitle,
                LikeCount = topQuoteRaw.LikeCount
            };
        }

        // ── Foydalanuvchi ma'lumoti ─────────────────────────────────────────────────
        var user = await _db.Users
            .Where(u => u.Id == uid)
            .Select(u => new { u.FullName, u.Username, u.AvatarUrl })
            .FirstOrDefaultAsync(ct);

        // ── Motivatsiya + dizayn ────────────────────────────────────────────────────
        var motivation = YearReviewMotivation.For(uid, year, booksRead, totalPages, activeDays);

        return new YearReviewDto
        {
            Year = year,
            UserId = uid,
            FullName = user?.FullName ?? "Kitobxon",
            Username = user?.Username,
            AvatarUrl = user?.AvatarUrl,
            BooksRead = booksRead,
            TotalPages = totalPages,
            ActiveDays = activeDays,
            Books = books,
            TopPost = topPost,
            TopQuote = topQuote,
            Motivation = motivation.Message,
            ThemeVariant = motivation.ThemeVariant,
            Emojis = motivation.Emojis,
            PrimaryEmoji = motivation.PrimaryEmoji
        };
    }

    /// <summary>HTML teglarni olib tashlaydi va HTML entity'larni dekod qiladi.</summary>
    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(noTags);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    /// <summary>Matnni belgilangan uzunlikkacha qisqartiradi (so'z chegarasida, "…" bilan).</summary>
    private static string Snippet(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Trim();
        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = text[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut.TrimEnd() + "…";
    }
}
