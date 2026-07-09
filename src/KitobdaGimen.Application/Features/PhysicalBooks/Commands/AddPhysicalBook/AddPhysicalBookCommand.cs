using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.AddPhysicalBook;

/// <summary>
/// Foydalanuvchi o'zida mavjud kitobni almashish kutubxonasiga qo'shadi. Kitobni
/// katalogdan tanlash (<see cref="BookId"/>) yoki qo'lda nom/muallif kiritish mumkin.
/// </summary>
public record AddPhysicalBookCommand : IRequest<PhysicalBookDto>
{
    /// <summary>Katalogdagi kitob id'si (qidiruvdan tanlanganda). Qo'lda kiritilsa null.</summary>
    public int? BookId { get; init; }

    /// <summary>Katalogda yo'q kitob uchun qo'lda nom.</summary>
    public string? ManualTitle { get; init; }
    public string? ManualAuthor { get; init; }
}
