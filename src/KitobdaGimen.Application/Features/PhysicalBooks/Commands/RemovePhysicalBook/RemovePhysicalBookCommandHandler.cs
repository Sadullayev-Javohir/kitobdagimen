using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.RemovePhysicalBook;

public class RemovePhysicalBookCommandHandler : IRequestHandler<RemovePhysicalBookCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemovePhysicalBookCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RemovePhysicalBookCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Amal uchun tizimga kiring.");

        var book = await _db.PhysicalBooks
            .Include(p => p.Reservations)
            .FirstOrDefaultAsync(p => p.Id == request.PhysicalBookId, cancellationToken)
            ?? throw new NotFoundException("Kitob", request.PhysicalBookId);

        if (book.OwnerId != userId)
        {
            throw new ForbiddenAccessException("Faqat kitob egasi uni o'chira oladi.");
        }

        if (book.Status != PhysicalBookStatus.Mavjud)
        {
            throw new ForbiddenAccessException("Band qilingan yoki o'qilayotgan kitobni o'chirib bo'lmaydi.");
        }

        // Band qilish yozuvlari (tugagan/bekor qilinganlar) ham kitob bilan birga o'chadi.
        _db.PhysicalBookReservations.RemoveRange(book.Reservations);
        _db.PhysicalBooks.Remove(book);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
