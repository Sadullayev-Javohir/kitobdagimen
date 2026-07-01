namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>E'lon qilingan (yakunlangan) challenge g'olibi — like va sovg'a ma'lumoti bilan.</summary>
public record ChallengeWinnerDto
{
    public int Id { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public int Rank { get; init; }

    public int UserId { get; init; }
    public string FullName { get; init; } = null!;
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }

    public int PagesRead { get; init; }
    public int BooksRead { get; init; }
    public int ActiveDays { get; init; }
    public double AvgPagesPerDay { get; init; }

    public int LikeCount { get; init; }
    public bool LikedByCurrentUser { get; init; }

    // Sovg'a (faqat 1-o'rin, super admin kiritsa)
    public string? GiftBookTitle { get; init; }
    public string? GiftBookAuthor { get; init; }
    public string? GiftBookCoverUrl { get; init; }

    public DateTime AnnouncedAt { get; init; }
}

/// <summary>
/// E'lon qilingan challenge natijasi: davr, g'oliblar va e'lon "faol"ligi (24 soat ichida
/// bo'lsa — bayram rejimi va modal avtomatik ochiladi).
/// </summary>
public record AnnouncedChallengeDto
{
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>"Iyun 2026" kabi sarlavha.</summary>
    public string PeriodLabel { get; init; } = string.Empty;

    /// <summary>E'lon qilingan vaqt (UTC) — g'oliblar orasidagi eng oxirgisi.</summary>
    public DateTime AnnouncedAt { get; init; }

    /// <summary>E'lon so'nggi 24 soat ichida bo'lganmi (bayram rejimi + avtomatik modal).</summary>
    public bool IsAnnouncementActive { get; init; }

    /// <summary>1-, 2- va 3-o'rin g'oliblari (o'rin bo'yicha tartiblangan).</summary>
    public IReadOnlyList<ChallengeWinnerDto> Winners { get; init; } = Array.Empty<ChallengeWinnerDto>();
}
