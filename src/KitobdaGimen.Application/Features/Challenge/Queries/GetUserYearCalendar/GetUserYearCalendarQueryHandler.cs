using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserYearCalendar;

public class GetUserYearCalendarQueryHandler
    : IRequestHandler<GetUserYearCalendarQuery, YearCalendarDto>
{
    private readonly IAppDbContext _db;

    public GetUserYearCalendarQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<YearCalendarDto> Handle(
        GetUserYearCalendarQuery request, CancellationToken cancellationToken)
    {
        var from = new DateOnly(request.Year, 1, 1);
        var to = new DateOnly(request.Year, 12, 31);

        var rows = await _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0
                        && p.ReadingGoal.UserId == request.UserId
                        && p.Date >= from && p.Date <= to)
            .Select(p => new { p.Date, p.PagesReadToday, p.ReadingGoal.BookId })
            .ToListAsync(cancellationToken);

        return ChallengeStats.BuildYearCalendar(
            rows.Select(r => new ChallengeStats.Row(r.Date, r.PagesReadToday, r.BookId)),
            request.Year);
    }
}
