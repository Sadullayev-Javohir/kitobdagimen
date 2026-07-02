namespace KitobdaGimen.Application.Features.Home.Queries.GetLandingStats;

/// <summary>
/// Landing sahifadagi ommaviy statistika (agregat, maxfiy ma'lumotsiz).
/// AppSettings'da JSON snapshot sifatida saqlanadi va kuniga bir marta yangilanadi.
/// </summary>
public record LandingStatsDto
{
    /// <summary>Ro'yxatdan o'tgan foydalanuvchilar soni.</summary>
    public int UserCount { get; init; }

    /// <summary>O'qib tugatilgan kitoblar soni (CurrentPage >= TotalPages bo'lgan maqsadlar).</summary>
    public int BooksRead { get; init; }

    /// <summary>Jami o'qilgan betlar (ReadingProgress kunlik yozuvlari yig'indisi).</summary>
    public long PagesRead { get; init; }

    /// <summary>Snapshot qachon hisoblangan (UTC) — kunlik yangilanishni aniqlash uchun.</summary>
    public DateTime UpdatedAtUtc { get; init; }
}
