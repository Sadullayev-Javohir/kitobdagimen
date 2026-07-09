using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReservePhysicalBook;

/// <summary>
/// Boshqa foydalanuvchi "O'qimoqchiman" tugmasini bosganda kitobni 24 soatga band qiladi.
/// Status "Mavjud" bo'lgan va o'zining kitobi bo'lmagan holdagina ishlaydi.
/// </summary>
public record ReservePhysicalBookCommand(int PhysicalBookId) : IRequest<PhysicalBookDto>;
