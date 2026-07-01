using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeBoard;

public class GetChallengeBoardQueryHandler
    : IRequestHandler<GetChallengeBoardQuery, ChallengeBoardDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _mediator;

    public GetChallengeBoardQueryHandler(
        IAppDbContext db, ICurrentUserService currentUser, ISender mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ChallengeBoardDto> Handle(
        GetChallengeBoardQuery request, CancellationToken cancellationToken)
    {
        if (ChallengeCalendar.IsBeforeStart(request.Year, request.Month))
        {
            return new ChallengeBoardDto();
        }

        var listSize = Math.Max(1, request.PodiumCount + request.ListCount);

        // Podium (top 3) + keyingi 20 — bitta reyting so'rovi bilan.
        var top = await _mediator.Send(new GetChallengeStandingsQuery
        {
            Year = request.Year,
            Month = request.Month,
            Limit = listSize
        }, cancellationToken);

        var podium = top.Where(t => t.Rank <= request.PodiumCount).ToList();
        var others = top.Where(t => t.Rank > request.PodiumCount).ToList();

        var myStanding = await ComputeMyStandingAsync(request, top, cancellationToken);

        return new ChallengeBoardDto
        {
            Podium = podium,
            Others = others,
            MyStanding = myStanding
        };
    }

    /// <summary>
    /// Joriy foydalanuvchi ko'rsatilgan ro'yxatdan (top) tashqarida bo'lsa, uning shaxsiy o'rnini
    /// (butun reyting bo'yicha) hisoblab qaytaradi. Aks holda (ro'yxatda bor yoki umuman
    /// o'qimagan / anonim) null.
    /// </summary>
    private async Task<ChallengeStandingDto?> ComputeMyStandingAsync(
        GetChallengeBoardQuery request,
        IReadOnlyList<ChallengeStandingDto> top,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int uid)
        {
            return null;
        }

        // Ro'yxatda allaqachon ko'rinsa — alohida qatorga hojat yo'q.
        if (top.Any(t => t.UserId == uid))
        {
            return null;
        }

        var (from, to) = ChallengeCalendar.Range(request.Year, request.Month);
        var elapsedDays = Math.Max(1, ChallengeCalendar.ElapsedDays(request.Year, request.Month));

        var progress = _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0 && p.Date >= from && p.Date <= to);

        var myAgg = await progress
            .Where(p => p.ReadingGoal.UserId == uid)
            .GroupBy(p => p.ReadingGoal.UserId)
            .Select(g => new
            {
                Pages = g.Sum(p => p.PagesReadToday),
                ActiveDays = g.Select(p => p.Date).Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Bu davrda umuman o'qimagan bo'lsa — reytingda emas.
        if (myAgg is null || myAgg.Pages <= 0)
        {
            return null;
        }

        var myBooks = await progress
            .Where(p => p.ReadingGoal.UserId == uid)
            .Select(p => p.ReadingGoal.BookId)
            .Distinct()
            .CountAsync(cancellationToken);

        // O'rin = mendan ko'proq bet o'qiganlar soni + teng bo'lganda kichik userId (tartiblash
        // qoidasi: Pages desc, keyin UserId asc) + 1.
        var perUser = progress
            .GroupBy(p => p.ReadingGoal.UserId)
            .Select(g => new { UserId = g.Key, Pages = g.Sum(p => p.PagesReadToday) });

        var above = await perUser
            .CountAsync(
                x => x.Pages > myAgg.Pages || (x.Pages == myAgg.Pages && x.UserId < uid),
                cancellationToken);

        var myRank = above + 1;

        var me = await _db.Users
            .Where(u => u.Id == uid)
            .Select(u => new { u.FullName, u.Username, u.AvatarUrl, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        return new ChallengeStandingDto
        {
            UserId = uid,
            FullName = me?.FullName ?? "Foydalanuvchi",
            Username = me?.Username,
            AvatarUrl = AvatarPrivacy.Resolve(me?.Email, me?.AvatarUrl, viewerEmail),
            Rank = myRank,
            PagesRead = myAgg.Pages,
            BooksRead = myBooks,
            ActiveDays = myAgg.ActiveDays,
            AvgPagesPerDay = Math.Round((double)myAgg.Pages / elapsedDays, 1)
        };
    }
}
