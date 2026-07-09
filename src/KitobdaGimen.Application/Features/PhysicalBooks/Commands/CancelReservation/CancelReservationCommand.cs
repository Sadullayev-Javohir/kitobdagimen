using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.CancelReservation;

/// <summary>
/// Band qilishni bekor qiladi. Band qilgan foydalanuvchi fikridan qaytsa yoki egasi
/// band qilishni rad etsa ishlatiladi. Status "Band qilindi" -> "Mavjud".
/// </summary>
public record CancelReservationCommand(int PhysicalBookId) : IRequest<PhysicalBookDto>;
