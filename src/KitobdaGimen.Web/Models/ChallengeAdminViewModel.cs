using KitobdaGimen.Application.Features.Challenge.Dtos;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Web.Models;

/// <summary>
/// /challenge/admin — admin/super admin uchun oldindan ko'rish (preview) sahifasi modeli.
/// Admin g'oliblar qanday ko'rinishini oldindan ko'radi, oyni e'lon qiladi; super admin
/// esa 1-o'rin g'olibiga kitob sovg'a qiladi.
/// </summary>
public class ChallengeAdminViewModel
{
    /// <summary>Ko'rilayotgan davr — yil.</summary>
    public int Year { get; init; }

    /// <summary>Ko'rilayotgan davr — oy.</summary>
    public int Month { get; init; }

    public string PeriodLabel { get; init; } = string.Empty;

    /// <summary>Bu davr uchun jonli/hisoblangan reyting (top 3) — preview.</summary>
    public IReadOnlyList<ChallengeStandingDto> Standings { get; init; } = Array.Empty<ChallengeStandingDto>();

    /// <summary>Bu davr allaqachon e'lon qilinganmi (agar ha — g'oliblar shu yerda).</summary>
    public AnnouncedChallengeDto? Announced { get; init; }

    /// <summary>Davr yakunlanganmi (oy tugaganmi).</summary>
    public bool IsPeriodCompleted { get; init; }

    /// <summary>Dekoratsiya uchun kitob muqovalari (preview modalida ham ishlatiladi).</summary>
    public IReadOnlyList<string> DecorationCovers { get; init; } = Array.Empty<string>();

    public UserRole MyRole { get; init; }
    public bool IsSuperAdmin => MyRole == UserRole.SuperAdmin;
}
