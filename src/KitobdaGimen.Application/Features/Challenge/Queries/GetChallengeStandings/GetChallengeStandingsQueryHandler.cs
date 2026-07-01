using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;

public class GetChallengeStandingsQueryHandler
    : IRequestHandler<GetChallengeStandingsQuery, IReadOnlyList<ChallengeStandingDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetChallengeStandingsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ChallengeStandingDto>> Handle(
        GetChallengeStandingsQuery request, CancellationToken cancellationToken)
    {
        // Challenge boshlanishidan (Iyul 2026) oldingi oylar uchun reyting ko'rsatilmaydi.
        if (ChallengeCalendar.IsBeforeStart(request.Year, request.Month))
        {
            return Array.Empty<ChallengeStandingDto>();
        }

        var (from, to) = ChallengeCalendar.Range(request.Year, request.Month);
        var elapsedDays = Math.Max(1, ChallengeCalendar.ElapsedDays(request.Year, request.Month));

        var progress = _db.ReadingProgress
            .Where(p => p.PagesReadToday > 0 && p.Date >= from && p.Date <= to);

        // 1-bosqich: foydalanuvchi bo'yicha jami betlar, top N.
        var totals = await progress
            .GroupBy(p => p.ReadingGoal.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Pages = g.Sum(p => p.PagesReadToday),
                ActiveDays = g.Select(p => p.Date).Distinct().Count()
            })
            .OrderByDescending(x => x.Pages)
            .ThenBy(x => x.UserId)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
        {
            return Array.Empty<ChallengeStandingDto>();
        }

        var userIds = totals.Select(t => t.UserId).ToList();

        // 2-bosqich: alohida kitoblar soni.
        var bookCounts = await progress
            .Where(p => userIds.Contains(p.ReadingGoal.UserId))
            .Select(p => new { p.ReadingGoal.UserId, p.ReadingGoal.BookId })
            .Distinct()
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Books = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Books, cancellationToken);

        // 3-bosqich: profil ma'lumotlari.
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Username, u.AvatarUrl, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        var result = new List<ChallengeStandingDto>(totals.Count);
        var rank = 0;
        foreach (var t in totals)
        {
            rank++;
            users.TryGetValue(t.UserId, out var u);
            bookCounts.TryGetValue(t.UserId, out var books);

            result.Add(new ChallengeStandingDto
            {
                UserId = t.UserId,
                FullName = u?.FullName ?? "Foydalanuvchi",
                Username = u?.Username,
                AvatarUrl = AvatarPrivacy.Resolve(u?.Email, u?.AvatarUrl, viewerEmail),
                Rank = rank,
                PagesRead = t.Pages,
                BooksRead = books,
                ActiveDays = t.ActiveDays,
                AvgPagesPerDay = Math.Round((double)t.Pages / elapsedDays, 1)
            });
        }

        return result;
    }
}
