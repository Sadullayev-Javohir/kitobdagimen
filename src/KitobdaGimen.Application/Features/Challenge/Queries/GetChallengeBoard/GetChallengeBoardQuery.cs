using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeBoard;

/// <summary>
/// Berilgan davr (yil+oy) uchun kengaytirilgan reyting jadvali: g'oliblar (top 3) va ulardan
/// keyingi <see cref="ListCount"/> kitobxon. Agar joriy foydalanuvchi ko'rsatilgan ro'yxatdan
/// tashqarida bo'lsa, uning shaxsiy o'rni ham hisoblanib qaytariladi.
/// </summary>
public record GetChallengeBoardQuery : IRequest<ChallengeBoardDto>
{
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>G'oliblar (podium) soni — default 3.</summary>
    public int PodiumCount { get; init; } = 3;

    /// <summary>Podiumdan keyin ko'rsatiladigan qo'shimcha kitobxonlar soni — default 20.</summary>
    public int ListCount { get; init; } = 20;
}
