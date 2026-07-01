using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetRandomBookCovers;

public class GetRandomBookCoversQueryHandler
    : IRequestHandler<GetRandomBookCoversQuery, IReadOnlyList<string>>
{
    private readonly IAppDbContext _db;

    public GetRandomBookCoversQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> Handle(
        GetRandomBookCoversQuery request, CancellationToken cancellationToken)
    {
        var count = Math.Clamp(request.Count, 1, 60);

        // asaxiy.uz dan kelgan, muqovasi bor kitoblar — tasodifiy tartibda.
        var covers = await _db.Books
            .Where(b => b.Source == "asaxiy.uz" && b.CoverUrl != null && b.CoverUrl != "")
            .OrderBy(_ => EF.Functions.Random())
            .Select(b => b.CoverUrl!)
            .Take(count)
            .ToListAsync(cancellationToken);

        // Zaxira: agar asaxiy kitoblari yetarli bo'lmasa, muqovasi bor istalgan kitoblardan.
        if (covers.Count < count)
        {
            var extra = await _db.Books
                .Where(b => b.CoverUrl != null && b.CoverUrl != "")
                .OrderBy(_ => EF.Functions.Random())
                .Select(b => b.CoverUrl!)
                .Take(count)
                .ToListAsync(cancellationToken);

            covers = covers.Concat(extra).Distinct().Take(count).ToList();
        }

        return covers;
    }
}
