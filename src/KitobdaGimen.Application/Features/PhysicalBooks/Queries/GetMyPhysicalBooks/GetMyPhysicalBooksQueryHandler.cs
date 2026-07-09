using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetMyPhysicalBooks;

public class GetMyPhysicalBooksQueryHandler
    : IRequestHandler<GetMyPhysicalBooksQuery, IReadOnlyList<PhysicalBookDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyPhysicalBooksQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PhysicalBookDto>> Handle(
        GetMyPhysicalBooksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Kitoblaringizni ko'rish uchun tizimga kiring.");

        var books = await _db.PhysicalBooks
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.Book)
            .Include(p => p.Reservations)
                .ThenInclude(r => r.Reserver)
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return books.Select(b => PhysicalBookMapper.ToDto(b, userId)).ToList();
    }
}
