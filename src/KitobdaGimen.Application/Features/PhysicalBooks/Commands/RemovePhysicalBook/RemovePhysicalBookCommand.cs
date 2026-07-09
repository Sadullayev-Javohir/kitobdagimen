using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.RemovePhysicalBook;

/// <summary>
/// Kitob egasi kitobni almashish kutubxonasidan olib tashlaydi. Faqat kitob "Mavjud"
/// holatida (band qilinmagan yoki o'qilmayotgan) bo'lsa o'chirish mumkin.
/// </summary>
public record RemovePhysicalBookCommand(int PhysicalBookId) : IRequest<Unit>;
