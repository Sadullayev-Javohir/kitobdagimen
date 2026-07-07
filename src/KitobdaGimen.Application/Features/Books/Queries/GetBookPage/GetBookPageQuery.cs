using MediatR;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBookPage;

/// <summary>
/// Bitta kitobning ommaviy (Google indekslaydigan) sahifasi uchun barcha ma'lumot:
/// kitob + unga yozilgan barcha taqrizlar va iqtiboslar. Kitob topilmasa null.
/// </summary>
public record GetBookPageQuery(int BookId) : IRequest<BookPageDto?>;

public record BookPageDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public string? CoverUrl { get; init; }
    public string? GenreName { get; init; }
    public string? Source { get; init; }
    public int TotalPages { get; init; }

    /// <summary>Bu kitobga taqriz yoki iqtibos yozgan noyob kitobxonlar soni.</summary>
    public int ReaderCount { get; init; }

    public IReadOnlyList<BookPagePostDto> Posts { get; init; } = Array.Empty<BookPagePostDto>();
    public IReadOnlyList<BookPageQuoteDto> Quotes { get; init; } = Array.Empty<BookPageQuoteDto>();
}

/// <summary>Kitob sahifasidagi taqriz kartochkasi; to'liq matn /post/{username}/{slug} da.</summary>
public record BookPagePostDto(
    int AuthorId, string AuthorName, string? AuthorUsername, string? AuthorAvatarUrl,
    string Slug, string ReviewText, DateTime CreatedAt, int LikeCount, int CommentCount);

/// <summary>Kitob sahifasidagi iqtibos kartochkasi; kanonik sahifasi /iqtibos/{username}/{slug}.</summary>
public record BookPageQuoteDto(
    int Id, int AuthorId, string AuthorName, string? AuthorUsername, string? AuthorAvatarUrl,
    string Slug, string Text, DateTime CreatedAt, int LikeCount, int CommentCount);
