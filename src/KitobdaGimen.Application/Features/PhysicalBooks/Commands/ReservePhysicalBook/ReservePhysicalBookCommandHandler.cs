using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReservePhysicalBook;

public class ReservePhysicalBookCommandHandler : IRequestHandler<ReservePhysicalBookCommand, PhysicalBookDto>
{
    /// <summary>Band qilish qancha vaqt amal qiladi.</summary>
    public static readonly TimeSpan ReservationDuration = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ReservePhysicalBookCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<PhysicalBookDto> Handle(ReservePhysicalBookCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Kitobni band qilish uchun tizimga kiring.");

        var book = await _db.PhysicalBooks
            .Include(p => p.Owner)
            .Include(p => p.Book)
            .Include(p => p.Reservations)
                .ThenInclude(r => r.Reserver)
            .FirstOrDefaultAsync(p => p.Id == request.PhysicalBookId, cancellationToken)
            ?? throw new NotFoundException("Kitob", request.PhysicalBookId);

        if (book.OwnerId == userId)
        {
            throw new ForbiddenAccessException("O'zingizning kitobingizni band qila olmaysiz.");
        }

        if (book.Status != PhysicalBookStatus.Mavjud)
        {
            throw new ForbiddenAccessException("Bu kitob hozir band qilish uchun mavjud emas.");
        }

        var now = DateTime.UtcNow;
        var reservation = new PhysicalBookReservation
        {
            PhysicalBookId = book.Id,
            ReserverId = userId,
            ReservedAt = now,
            ExpiresAt = now.Add(ReservationDuration),
            IsConfirmed = false
        };

        _db.PhysicalBookReservations.Add(reservation);
        book.Status = PhysicalBookStatus.BandQilindi;

        await _db.SaveChangesAsync(cancellationToken);

        // Egaga bildirishnoma — kimdir kitobini olmoqchi.
        var reserverName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Kimdir";
        var bookTitle = book.Book?.Title ?? book.ManualTitle ?? "kitobingiz";

        await _notifications.NotifyAsync(book.OwnerId, new NotificationDto
        {
            Type = "physical_book_reserved",
            ActorId = userId,
            ActorName = reserverName,
            Message = $"{reserverName} \"{bookTitle}\" kitobingizni o'qimoqchi. 24 soat ichida topshiring.",
            Url = "/almashish"
        }, cancellationToken);

        // Yangilangan holatni qaytaramiz (yangi band qilish reservation ro'yxatida).
        book.Reservations.Add(reservation);
        reservation.Reserver = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        return PhysicalBookMapper.ToDto(book, userId);
    }
}
