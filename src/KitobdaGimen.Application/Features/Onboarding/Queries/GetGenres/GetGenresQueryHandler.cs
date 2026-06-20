using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Onboarding.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Onboarding.Queries.GetGenres;

public class GetGenresQueryHandler : IRequestHandler<GetGenresQuery, IReadOnlyList<GenreDto>>
{
    private readonly IAppDbContext _db;

    public GetGenresQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GenreDto>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        return await _db.Genres
            .OrderBy(g => g.Name)
            .ProjectToType<GenreDto>()
            .ToListAsync(cancellationToken);
    }
}
