using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Leaderboard.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Leaderboard.Queries.GetReadingLeaderboard;

/// <summary>
/// Eng ko'p kitob o'qigan foydalanuvchilar reytingini hisoblaydi. O'qish "Kutubxona"
/// bo'limidan — ya'ni <see cref="Domain.Entities.ReadingProgress"/> (har kuni kiritilgan
/// betlar) orqali — o'lchanadi. Tanlangan davr ichida o'qilgan jami betlar bo'yicha
/// kamayish tartibida saralanadi; teng bo'lsa o'qilgan kitoblar soni hal qiladi.
/// "Bugun" — O'zbekiston (UTC+5) sanasiga ko'ra, kunlik eslatma jobi bilan bir xil.
/// </summary>
public class GetReadingLeaderboardQueryHandler
    : IRequestHandler<GetReadingLeaderboardQuery, IReadOnlyList<LeaderboardUserDto>>
{
    private readonly IAppDbContext _db;

    public GetReadingLeaderboardQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LeaderboardUserDto>> Handle(
        GetReadingLeaderboardQuery request, CancellationToken cancellationToken)
    {
        // O'zbekiston bugungi sanasi (Hangfire/server UTC'da ishlaydi).
        var uzToday = Common.UzTime.Today;

        // Davr boshlanish sanasi (null => umrlik, filtrsiz).
        DateOnly? from = request.Period switch
        {
            LeaderboardPeriod.Daily => uzToday,
            LeaderboardPeriod.Weekly => uzToday.AddDays(-6),   // so'nggi 7 kun (bugun bilan)
            LeaderboardPeriod.Monthly => uzToday.AddDays(-29), // so'nggi 30 kun (bugun bilan)
            _ => null
        };

        var progress = _db.ReadingProgress.Where(p => p.PagesReadToday > 0);
        if (from is not null)
        {
            progress = progress.Where(p => p.Date >= from.Value && p.Date <= uzToday);
        }

        // 1-bosqich: foydalanuvchi bo'yicha jami o'qilgan betlar, top N.
        var totals = await progress
            .GroupBy(p => p.ReadingGoal.UserId)
            .Select(g => new { UserId = g.Key, Pages = g.Sum(p => p.PagesReadToday) })
            .OrderByDescending(x => x.Pages)
            .ThenBy(x => x.UserId)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
        {
            return Array.Empty<LeaderboardUserDto>();
        }

        var userIds = totals.Select(t => t.UserId).ToList();

        // 2-bosqich: top foydalanuvchilarning davr ichida o'qigan (alohida) kitoblari soni.
        var bookCounts = await progress
            .Where(p => userIds.Contains(p.ReadingGoal.UserId))
            .Select(p => new { p.ReadingGoal.UserId, p.ReadingGoal.BookId })
            .Distinct()
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Books = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Books, cancellationToken);

        // 3-bosqich: foydalanuvchi profil ma'lumotlari.
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Username, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = new List<LeaderboardUserDto>(totals.Count);
        var rank = 0;
        foreach (var t in totals)
        {
            rank++;
            users.TryGetValue(t.UserId, out var u);
            bookCounts.TryGetValue(t.UserId, out var books);

            result.Add(new LeaderboardUserDto
            {
                UserId = t.UserId,
                FullName = u?.FullName ?? "Foydalanuvchi",
                Username = u?.Username,
                AvatarUrl = u?.AvatarUrl,
                Rank = rank,
                Score = t.Pages,
                Detail = $"{books} kitob"
            });
        }

        return result;
    }
}
