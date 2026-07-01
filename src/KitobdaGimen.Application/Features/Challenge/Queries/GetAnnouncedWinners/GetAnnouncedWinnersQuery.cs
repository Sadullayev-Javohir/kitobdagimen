using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetAnnouncedWinners;

/// <summary>
/// E'lon qilingan (yakunlangan) challenge g'oliblarini qaytaradi. Davr ko'rsatilmasa —
/// eng oxirgi e'lon qilingan oy olinadi. G'olib topilmasa null qaytadi.
/// </summary>
public record GetAnnouncedWinnersQuery : IRequest<AnnouncedChallengeDto?>
{
    public int? Year { get; init; }
    public int? Month { get; init; }
}
