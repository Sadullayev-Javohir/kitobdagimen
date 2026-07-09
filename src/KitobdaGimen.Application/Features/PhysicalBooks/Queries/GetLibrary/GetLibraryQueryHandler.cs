using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetLibrary;

public class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, IReadOnlyList<PhysicalBookDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetLibraryQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PhysicalBookDto>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = _db.PhysicalBooks
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.Book)
            // Faqat boshqa foydalanuvchilarning mavjud kitoblari.
            .Where(p => p.Status == PhysicalBookStatus.Mavjud && p.OwnerId != userId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                (p.Book != null && (p.Book.Title.ToLower().Contains(term) || p.Book.Author.ToLower().Contains(term))) ||
                (p.ManualTitle != null && p.ManualTitle.ToLower().Contains(term)) ||
                (p.ManualAuthor != null && p.ManualAuthor.ToLower().Contains(term)));
        }

        var books = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Mavjud kitoblarda faol band qilish yo'q — Reservations'ni yuklamaymiz.
        return books.Select(b => PhysicalBookMapper.ToDto(b, userId)).ToList();
    }
}
