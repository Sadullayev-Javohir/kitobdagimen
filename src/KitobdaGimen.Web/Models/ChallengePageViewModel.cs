using KitobdaGimen.Application.Features.Challenge.Dtos;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Web.Models;

/// <summary>
/// /challenge sahifasi uchun ko'rinish modeli: joriy oy jonli reytingi, oxirgi e'lon
/// qilingan g'oliblar (like + sovg'a bilan), shaxsiy statistika (three.js) va dekoratsiya
/// uchun tasodifiy kitob muqovalari.
/// </summary>
public class ChallengePageViewModel
{
    /// <summary>Joriy davr — yil.</summary>
    public int CurrentYear { get; init; }

    /// <summary>Joriy davr — oy (1..12).</summary>
    public int CurrentMonth { get; init; }

    /// <summary>"Iyul 2026" kabi joriy davr sarlavhasi.</summary>
    public string CurrentPeriodLabel { get; init; } = string.Empty;

    /// <summary>Joriy oy jonli reytingi (top 3).</summary>
    public IReadOnlyList<ChallengeStandingDto> LiveStandings { get; init; } = Array.Empty<ChallengeStandingDto>();

    /// <summary>Oxirgi e'lon qilingan g'oliblar (bo'lmasa null).</summary>
    public AnnouncedChallengeDto? Announced { get; init; }

    /// <summary>Joriy foydalanuvchining o'qish statistikasi (30 kun + yillik heatmap kalendar).</summary>
    public UserChallengeStatsDto Stats { get; init; } = new();

    /// <summary>Dekoratsiya uchun kitob muqovalari (asaxiy.uz).</summary>
    public IReadOnlyList<string> DecorationCovers { get; init; } = Array.Empty<string>();

    /// <summary>Joriy foydalanuvchi id'si (like tugmasi holati uchun).</summary>
    public int? CurrentUserId { get; init; }

    /// <summary>Joriy foydalanuvchi roli (admin havolasini ko'rsatish uchun).</summary>
    public UserRole MyRole { get; init; }
}
