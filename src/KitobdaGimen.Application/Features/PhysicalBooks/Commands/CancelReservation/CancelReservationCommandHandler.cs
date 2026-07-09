using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.CancelReservation;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, PhysicalBookDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public CancelReservationCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<PhysicalBookDto> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Amal uchun tizimga kiring.");

        var book = await _db.PhysicalBooks
            .Include(p => p.Owner)
            .Include(p => p.Book)
            .Include(p => p.Reservations)
                .ThenInclude(r => r.Reserver)
            .FirstOrDefaultAsync(p => p.Id == request.PhysicalBookId, cancellationToken)
            ?? throw new NotFoundException("Kitob", request.PhysicalBookId);

        if (book.Status != PhysicalBookStatus.BandQilindi)
        {
            throw new ForbiddenAccessException("Bu kitob band qilinmagan.");
        }

        var reservation = book.Reservations
            .Where(r => !r.IsConfirmed)
            .OrderByDescending(r => r.ReservedAt)
            .FirstOrDefault()
            ?? throw new ForbiddenAccessException("Faol band qilish topilmadi.");

        // Faqat band qilgan foydalanuvchi yoki kitob egasi bekor qila oladi.
        var isReserver = reservation.ReserverId == userId;
        var isOwner = book.OwnerId == userId;
        if (!isReserver && !isOwner)
        {
            throw new ForbiddenAccessException("Bu band qilishni bekor qilishga ruxsatingiz yo'q.");
        }

        _db.PhysicalBookReservations.Remove(reservation);
        book.Status = PhysicalBookStatus.Mavjud;
        book.Reservations.Remove(reservation);

        await _db.SaveChangesAsync(cancellationToken);

        // Qarshi tomonni xabardor qilamiz.
        var bookTitle = book.Book?.Title ?? book.ManualTitle ?? "kitob";
        if (isOwner)
        {
            // Egasi rad etdi -> band qilgan foydalanuvchiga xabar.
            await _notifications.NotifyAsync(reservation.ReserverId, new NotificationDto
            {
                Type = "physical_book_cancelled",
                ActorId = book.OwnerId,
                ActorName = book.Owner.FullName,
                Message = $"\"{bookTitle}\" uchun band qilishingiz egasi tomonidan bekor qilindi.",
                Url = "/almashish"
            }, cancellationToken);
        }
        else
        {
            // Band qilgan foydalanuvchi voz kechdi -> egaga xabar.
            await _notifications.NotifyAsync(book.OwnerId, new NotificationDto
            {
                Type = "physical_book_cancelled",
                ActorId = userId,
                ActorName = reservation.Reserver.FullName,
                Message = $"\"{bookTitle}\" uchun band qilish bekor qilindi. Kitob yana mavjud.",
                Url = "/almashish"
            }, cancellationToken);
        }

        return PhysicalBookMapper.ToDto(book, userId);
    }
}
