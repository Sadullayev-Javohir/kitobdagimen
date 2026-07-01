namespace KitobdaGimen.Application.Features.Challenge.Dtos;

/// <summary>
/// /challenge sahifasidagi kengaytirilgan reyting jadvali: g'oliblar (top 3, podium),
/// ulardan keyingi 20 kitobxon (4–23-o'rinlar) va — agar joriy foydalanuvchi shu ro'yxatdan
/// tashqarida bo'lsa — uning shaxsiy o'rni ("... 40-o'rin" kabi). Bularning barchasi jonli
/// (joriy davr) reytingidan hisoblanadi.
/// </summary>
public record ChallengeBoardDto
{
    /// <summary>G'oliblar — 1-, 2-, 3-o'rin (podium).</summary>
    public IReadOnlyList<ChallengeStandingDto> Podium { get; init; } = Array.Empty<ChallengeStandingDto>();

    /// <summary>Podiumdan keyingi kitobxonlar (odatda 4–23-o'rin — 20 ta).</summary>
    public IReadOnlyList<ChallengeStandingDto> Others { get; init; } = Array.Empty<ChallengeStandingDto>();

    /// <summary>
    /// Joriy foydalanuvchining shaxsiy o'rni — faqat u ko'rsatilgan ro'yxatdan
    /// (podium + others) tashqarida bo'lsa to'ldiriladi. Aks holda null.
    /// </summary>
    public ChallengeStandingDto? MyStanding { get; init; }
}
