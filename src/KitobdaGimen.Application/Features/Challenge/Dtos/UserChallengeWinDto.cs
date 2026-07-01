namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>Profil sahifasida ko'rsatiladigan challenge g'oliblik yozuvi.</summary>
public record UserChallengeWinDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public int Rank { get; init; }

    public int PagesRead { get; init; }
    public int BooksRead { get; init; }
    public int LikeCount { get; init; }

    // Sovg'a (faqat 1-o'rin)
    public string? GiftBookTitle { get; init; }
    public string? GiftBookAuthor { get; init; }
    public string? GiftBookCoverUrl { get; init; }
}
