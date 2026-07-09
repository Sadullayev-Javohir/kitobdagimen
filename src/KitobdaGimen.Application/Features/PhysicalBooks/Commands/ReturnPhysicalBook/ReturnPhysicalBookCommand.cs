using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReturnPhysicalBook;

/// <summary>
/// Kitob egasi kitob qaytarib olinganini bildiradi. Status "O'qiyapti" -> "Mavjud".
/// </summary>
public record ReturnPhysicalBookCommand(int PhysicalBookId) : IRequest<PhysicalBookDto>;
