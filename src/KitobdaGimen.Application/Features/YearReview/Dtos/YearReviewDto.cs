namespace KitobdaGimen.Application.Features.YearReview.Dtos;

/// <summary>
/// Foydalanuvchining bir yillik "Yillik Kitob Yakuni" hisoboti — kartochkada ko'rsatiladigan
/// barcha ma'lumot. Kartochka 20-dekabrdan 1-yanvargacha har kirishda modal ko'rinishida chiqadi,
/// ulashsa bo'ladi va rasm/PDF sifatida yuklab olinadi.
/// </summary>
public record YearReviewDto
{
    /// <summary>Hisobot yili (masalan 2026).</summary>
    public int Year { get; init; }

    // ── Foydalanuvchi ──────────────────────────────────────────────────────────────
    public int UserId { get; init; }
    public string FullName { get; init; } = "";
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }

    // ── Asosiy statistika ────────────────────────────────────────────────────────────
    /// <summary>Yil davomida o'qilgan (alohida) kitoblar soni.</summary>
    public int BooksRead { get; init; }

    /// <summary>Yil davomida o'qilgan jami betlar.</summary>
    public int TotalPages { get; init; }

    /// <summary>Yil davomida kamida bir bet o'qilgan kunlar soni.</summary>
    public int ActiveDays { get; init; }

    /// <summary>Yil davomida o'qilgan kitoblar (eng ko'p bet o'qilgani birinchi, cheklangan son).</summary>
    public IReadOnlyList<YearReviewBookDto> Books { get; init; } = Array.Empty<YearReviewBookDto>();

    // ── Eng ko'p like yig'gan post va iqtibos ────────────────────────────────────────
    public YearReviewTopPostDto? TopPost { get; init; }
    public YearReviewTopQuoteDto? TopQuote { get; init; }

    // ── Motivatsiya va dizayn ────────────────────────────────────────────────────────
    /// <summary>Foydalanuvchiga xos noyob motivatsion xabar.</summary>
    public string Motivation { get; init; } = "";

    /// <summary>Dizayn varianti (0..7) — year-review.css dagi .yr-theme-N.</summary>
    public int ThemeVariant { get; init; }

    /// <summary>Bayram emoji to'plami (kartochka aksentlari uchun).</summary>
    public IReadOnlyList<string> Emojis { get; init; } = Array.Empty<string>();

    /// <summary>Asosiy (birlamchi) emoji.</summary>
    public string PrimaryEmoji { get; init; } = "📚";

    /// <summary>Foydalanuvchida shu yil uchun ko'rsatishga arziydigan faoliyat bormi.</summary>
    public bool HasActivity =>
        BooksRead > 0 || TotalPages > 0 || TopPost is not null || TopQuote is not null;
}

/// <summary>Yillik yakunda ko'rsatiladigan bitta kitob.</summary>
public record YearReviewBookDto
{
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";
    public string? CoverUrl { get; init; }

    /// <summary>Shu kitobda yil davomida o'qilgan betlar.</summary>
    public int Pages { get; init; }
}

/// <summary>Yil davomida eng ko'p like yig'gan post.</summary>
public record YearReviewTopPostDto
{
    public string Slug { get; init; } = "";
    public string? Username { get; init; }

    /// <summary>Post matni (HTML olib tashlangan, qisqartirilgan).</summary>
    public string Snippet { get; init; } = "";
    public string BookTitle { get; init; } = "";
    public int LikeCount { get; init; }
}

/// <summary>Yil davomida eng ko'p like yig'gan iqtibos.</summary>
public record YearReviewTopQuoteDto
{
    public int Id { get; init; }

    /// <summary>Iqtibos matni (qisqartirilgan).</summary>
    public string Snippet { get; init; } = "";
    public string BookTitle { get; init; } = "";
    public int LikeCount { get; init; }
}
