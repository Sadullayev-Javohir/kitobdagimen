using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetMyPhysicalBooks;

/// <summary>
/// Joriy foydalanuvchi qo'shgan barcha jismoniy kitoblar (barcha statuslar bilan) —
/// "Mening kitoblarim" bo'limi uchun.
/// </summary>
public record GetMyPhysicalBooksQuery : IRequest<IReadOnlyList<PhysicalBookDto>>;
