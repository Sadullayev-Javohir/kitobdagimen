namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>
/// Foydalanuvchining o'qish statistikasi — three.js 3D grafikada ko'rsatish uchun.
/// Ikki ko'rinish: so'nggi 30 kunlik (kunlik) va so'nggi 12 oylik (oylik).
/// </summary>
public record UserChallengeStatsDto
{
    /// <summary>So'nggi 30 kun — har kuni o'qilgan betlar (eng eskisidan bugungacha).</summary>
    public IReadOnlyList<DailyStatDto> Daily { get; init; } = Array.Empty<DailyStatDto>();

    /// <summary>So'nggi 12 oy — har oyning o'qilgan betlari va kitoblari.</summary>
    public IReadOnlyList<MonthlyStatDto> Monthly { get; init; } = Array.Empty<MonthlyStatDto>();

    /// <summary>So'nggi 30 kunda o'qilgan jami betlar.</summary>
    public int MonthPages { get; init; }

    /// <summary>So'nggi 30 kunda kitob o'qilgan kunlar soni.</summary>
    public int MonthActiveDays { get; init; }

    /// <summary>So'nggi 12 oyda o'qilgan jami betlar.</summary>
    public int YearPages { get; init; }

    /// <summary>So'nggi 12 oyda kitob o'qilgan oylar soni.</summary>
    public int YearActiveMonths { get; init; }
}

/// <summary>Bir kunlik o'qish yozuvi (statistika uchun).</summary>
public record DailyStatDto
{
    public DateOnly Date { get; init; }
    public int Pages { get; init; }
    public bool Read => Pages > 0;
}

/// <summary>Bir oylik o'qish yig'indisi (statistika uchun).</summary>
public record MonthlyStatDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Pages { get; init; }
    public int Books { get; init; }
    public bool Read => Pages > 0;
}
