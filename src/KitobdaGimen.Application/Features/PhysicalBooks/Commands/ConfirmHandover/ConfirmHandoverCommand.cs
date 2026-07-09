using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ConfirmHandover;

/// <summary>
/// Kitob egasi kitobni band qilgan foydalanuvchiga topshirganini tasdiqlaydi.
/// Status "Band qilindi" -> "O'qiyapti"ga o'zgaradi.
/// </summary>
public record ConfirmHandoverCommand(int PhysicalBookId) : IRequest<PhysicalBookDto>;
