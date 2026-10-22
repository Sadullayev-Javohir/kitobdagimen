using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Books.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Books.Queries.GetBooks;

public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, IReadOnlyList<BookDto>>
{
    private readonly IAppDbContext _db;

    public GetBooksQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BookDto>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);
        var query = _db.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.WhereMatchesBookSearch(request.Search);
        }

        return await query
            .OrderBy(b => b.Title)
            .Take(limit)
            .ProjectToType<BookDto>()
            .ToListAsync(cancellationToken);
    }
}
