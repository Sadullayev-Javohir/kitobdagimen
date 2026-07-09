using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ConfirmHandover;

public class ConfirmHandoverCommandHandler : IRequestHandler<ConfirmHandoverCommand, PhysicalBookDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ConfirmHandoverCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<PhysicalBookDto> Handle(ConfirmHandoverCommand request, CancellationToken cancellationToken)
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

        if (book.OwnerId != userId)
        {
            throw new ForbiddenAccessException("Faqat kitob egasi topshirishni tasdiqlay oladi.");
        }

        if (book.Status != PhysicalBookStatus.BandQilindi)
        {
            throw new ForbiddenAccessException("Bu kitob band qilinmagan.");
        }

        // Joriy (eng so'nggi, tasdiqlanmagan) band qilishni topamiz.
        var reservation = book.Reservations
            .Where(r => !r.IsConfirmed)
            .OrderByDescending(r => r.ReservedAt)
            .FirstOrDefault()
            ?? throw new ForbiddenAccessException("Faol band qilish topilmadi.");

        reservation.IsConfirmed = true;
        book.Status = PhysicalBookStatus.OqiyApti;

        await _db.SaveChangesAsync(cancellationToken);

        // Band qilgan foydalanuvchiga xabar — kitob topshirildi.
        var bookTitle = book.Book?.Title ?? book.ManualTitle ?? "kitob";
        await _notifications.NotifyAsync(reservation.ReserverId, new NotificationDto
        {
            Type = "physical_book_handover",
            ActorId = book.OwnerId,
            ActorName = book.Owner.FullName,
            Message = $"\"{bookTitle}\" sizga topshirildi. Yoqimli mutolaa! 📖",
            Url = "/almashish"
        }, cancellationToken);

        return PhysicalBookMapper.ToDto(book, userId);
    }
}
