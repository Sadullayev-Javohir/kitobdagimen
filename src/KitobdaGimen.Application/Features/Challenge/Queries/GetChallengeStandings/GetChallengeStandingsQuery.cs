using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;

/// <summary>
/// Berilgan davr (yil+oy) uchun eng ko'p o'qigan kitobxonlar reytingi. Davr — kalendar oyi
/// (1-kundan oxirgi kungacha). Joriy oy uchun jonli natija, yakunlangan oy uchun o'sha oydagi
/// yakuniy natija. Betlar bo'yicha kamayish tartibida; teng bo'lsa o'qilgan kitoblar soni.
/// </summary>
public record GetChallengeStandingsQuery : IRequest<IReadOnlyList<ChallengeStandingDto>>
{
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>Nechta o'rin qaytarilsin (default: 3 — g'oliblar).</summary>
    public int Limit { get; init; } = 3;
}
