using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReturnPhysicalBook;

public class ReturnPhysicalBookCommandHandler : IRequestHandler<ReturnPhysicalBookCommand, PhysicalBookDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReturnPhysicalBookCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PhysicalBookDto> Handle(ReturnPhysicalBookCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenAccessException("Faqat kitob egasi qaytarilganini belgilay oladi.");
        }

        if (book.Status != PhysicalBookStatus.OqiyApti)
        {
            throw new ForbiddenAccessException("Bu kitob hozir o'qilmayapti.");
        }

        book.Status = PhysicalBookStatus.Mavjud;
        await _db.SaveChangesAsync(cancellationToken);

        return PhysicalBookMapper.ToDto(book, userId);
    }
}
