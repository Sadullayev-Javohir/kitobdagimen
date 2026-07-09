using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.AddPhysicalBook;

public class AddPhysicalBookCommandHandler : IRequestHandler<AddPhysicalBookCommand, PhysicalBookDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddPhysicalBookCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PhysicalBookDto> Handle(AddPhysicalBookCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Kitob qo'shish uchun tizimga kiring.");

        int? bookId = request.BookId;
        string? manualTitle = null;
        string? manualAuthor = null;
        string? manualCoverUrl = null;

        if (bookId is not null)
        {
            // Katalog id'si haqiqiy ekanini tekshiramiz (aks holda FK xatosi yuzaga kelardi).
            var exists = await _db.Books.AnyAsync(b => b.Id == bookId, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("Kitob", bookId);
            }
        }
        else
        {
            manualTitle = request.ManualTitle!.Trim();
            manualAuthor = string.IsNullOrWhiteSpace(request.ManualAuthor) ? null : request.ManualAuthor.Trim();
            manualCoverUrl = string.IsNullOrWhiteSpace(request.ManualCoverUrl) ? null : request.ManualCoverUrl.Trim();
        }

        var physicalBook = new PhysicalBook
        {
            OwnerId = userId,
            BookId = bookId,
            ManualTitle = manualTitle,
            ManualAuthor = manualAuthor,
            ManualCoverUrl = manualCoverUrl,
            Status = PhysicalBookStatus.Mavjud,
            CreatedAt = DateTime.UtcNow
        };

        _db.PhysicalBooks.Add(physicalBook);
        await _db.SaveChangesAsync(cancellationToken);

        // Egasi va (agar bor bo'lsa) katalog kitobini DTO uchun yuklab olamiz.
        var saved = await _db.PhysicalBooks
            .Include(p => p.Owner)
            .Include(p => p.Book)
            .Include(p => p.Reservations)
            .FirstAsync(p => p.Id == physicalBook.Id, cancellationToken);

        return PhysicalBookMapper.ToDto(saved, userId);
    }
}
