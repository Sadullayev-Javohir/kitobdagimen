using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Home.Queries.GetBackgroundVideoUrl;

public class GetBackgroundVideoUrlQueryHandler : IRequestHandler<GetBackgroundVideoUrlQuery, string?>
{
    private readonly IAppDbContext _db;

    public GetBackgroundVideoUrlQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> Handle(GetBackgroundVideoUrlQuery request, CancellationToken ct)
    {
        return await _db.AppSettings
            .AsNoTracking()
            .Where(s => s.Key == AppSettingKeys.BackgroundVideoUrl)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }
}
