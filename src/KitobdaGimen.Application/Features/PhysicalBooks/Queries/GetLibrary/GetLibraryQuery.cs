using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetLibrary;

/// <summary>
/// Almashish kutubxonasi: boshqa foydalanuvchilarning "Mavjud" kitoblari (band qilish uchun).
/// Ixtiyoriy qidiruv nom yoki muallif bo'yicha.
/// </summary>
public record GetLibraryQuery : IRequest<IReadOnlyList<PhysicalBookDto>>
{
    public string? Search { get; init; }
    public int Limit { get; init; } = 60;
}
