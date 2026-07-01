using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetRandomBookCovers;

/// <summary>
/// Dekoratsiya uchun asaxiy.uz'dan kelgan kitob muqovalari (tasodifiy). Challenge sahifasida
/// har tomonda uchib yuradigan kitob iconlari shu muqovalardan olinadi.
/// </summary>
public record GetRandomBookCoversQuery : IRequest<IReadOnlyList<string>>
{
    public int Count { get; init; } = 16;
}
