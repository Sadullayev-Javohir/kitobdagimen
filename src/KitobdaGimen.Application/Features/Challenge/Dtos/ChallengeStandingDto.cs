namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>
/// Challenge reytingidagi bitta kitobxon (jonli, joriy davr uchun yoki e'lon oldidan
/// ko'rish uchun). O'qish "Kutubxona" bo'limidan — kunlik betlar — orqali o'lchanadi.
/// </summary>
public record ChallengeStandingDto
{
    public int UserId { get; init; }
    public string FullName { get; init; } = null!;
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }

    /// <summary>O'rin (1-birinchi ...).</summary>
    public int Rank { get; init; }

    /// <summary>Davr ichida o'qilgan jami betlar.</summary>
    public int PagesRead { get; init; }

    /// <summary>Davr ichida o'qilgan (alohida) kitoblar soni.</summary>
    public int BooksRead { get; init; }

    /// <summary>Davr ichida kamida bir bet o'qilgan kunlar soni.</summary>
    public int ActiveDays { get; init; }

    /// <summary>Kuniga o'rtacha o'qilgan betlar.</summary>
    public double AvgPagesPerDay { get; init; }
}
