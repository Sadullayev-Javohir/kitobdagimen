namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>
/// Foydalanuvchining o'qish statistikasi — GitHub uslubidagi "heatmap" ko'rinishida
/// ko'rsatiladi. Ikki ko'rinish: so'nggi 30 kunlik (kunlik) va to'liq yillik kalendar
/// (avvalgi yillarni ham ko'rish mumkin).
/// </summary>
public record UserChallengeStatsDto
{
    /// <summary>So'nggi 30 kun — har kuni o'qilgan betlar/kitoblar (eng eskisidan bugungacha).</summary>
    public IReadOnlyList<DailyStatDto> Daily { get; init; } = Array.Empty<DailyStatDto>();

    /// <summary>So'nggi 30 kunda o'qilgan jami betlar.</summary>
    public int MonthPages { get; init; }

    /// <summary>So'nggi 30 kunda kitob o'qilgan kunlar soni.</summary>
    public int MonthActiveDays { get; init; }

    /// <summary>So'nggi 30 kunda o'qilgan turli kitoblar soni.</summary>
    public int MonthBooks { get; init; }

    /// <summary>Joriy yil bo'yicha to'liq kalendar (GitHub uslubidagi heatmap uchun).</summary>
    public YearCalendarDto Year { get; init; } = new();

    /// <summary>Foydalanuvchida o'qish yozuvi bo'lgan yillar (yangi → eski). Yil navigatsiyasi uchun.</summary>
    public IReadOnlyList<int> AvailableYears { get; init; } = Array.Empty<int>();
}

/// <summary>Bir yilning to'liq kalendari — har bir kun uchun o'qish yozuvi (heatmap uchun).</summary>
public record YearCalendarDto
{
    /// <summary>Yil (masalan 2026).</summary>
    public int Year { get; init; }

    /// <summary>1-yanvardan 31-dekabrgacha har bir kun (tartib bilan).</summary>
    public IReadOnlyList<DailyStatDto> Days { get; init; } = Array.Empty<DailyStatDto>();

    /// <summary>Yil davomida o'qilgan jami betlar.</summary>
    public int TotalPages { get; init; }

    /// <summary>Yil davomida o'qilgan turli kitoblar soni.</summary>
    public int TotalBooks { get; init; }

    /// <summary>Kitob o'qilgan kunlar soni.</summary>
    public int ActiveDays { get; init; }

    /// <summary>Eng ko'p o'qilgan kundagi betlar soni (rang darajalari uchun).</summary>
    public int MaxPages { get; init; }
}

/// <summary>Bir kunlik o'qish yozuvi (statistika uchun).</summary>
public record DailyStatDto
{
    public DateOnly Date { get; init; }
    public int Pages { get; init; }

    /// <summary>Shu kuni o'qilgan turli kitoblar soni.</summary>
    public int Books { get; init; }

    public bool Read => Pages > 0;
}
