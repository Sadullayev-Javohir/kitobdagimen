using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetAnnouncedWinners;

public class GetAnnouncedWinnersQueryHandler
    : IRequestHandler<GetAnnouncedWinnersQuery, AnnouncedChallengeDto?>
{
    /// <summary>E'lon "faol" hisoblanadigan oyna — e'londan keyingi 2 kun (48 soat, bayram rejimi).</summary>
    public static readonly TimeSpan AnnouncementWindow = TimeSpan.FromHours(48);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAnnouncedWinnersQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AnnouncedChallengeDto?> Handle(
        GetAnnouncedWinnersQuery request, CancellationToken cancellationToken)
    {
        int year, month;

        if (request.Year is int y && request.Month is int m)
        {
            year = y;
            month = m;
        }
        else
        {
            // Eng oxirgi e'lon qilingan davr (yil, keyin oy bo'yicha).
            var latest = await _db.ChallengeWinners
                .OrderByDescending(w => w.Year)
                .ThenByDescending(w => w.Month)
                .Select(w => new { w.Year, w.Month })
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is null)
            {
                return null;
            }

            year = latest.Year;
            month = latest.Month;
        }

        var currentUserId = _currentUser.UserId;

        var winners = await _db.ChallengeWinners
            .Where(w => w.Year == year && w.Month == month)
            .OrderBy(w => w.Rank)
            .Select(w => new ChallengeWinnerDto
            {
                Id = w.Id,
                Year = w.Year,
                Month = w.Month,
                Rank = w.Rank,
                UserId = w.UserId,
                FullName = w.User.FullName,
                Username = w.User.Username,
                AvatarUrl = w.User.AvatarUrl,
                PagesRead = w.PagesRead,
                BooksRead = w.BooksRead,
                ActiveDays = w.ActiveDays,
                AvgPagesPerDay = w.AvgPagesPerDay,
                LikeCount = w.Likes.Count,
                LikedByCurrentUser = currentUserId != null && w.Likes.Any(l => l.UserId == currentUserId),
                GiftBookTitle = w.GiftBookTitle,
                GiftBookAuthor = w.GiftBookAuthor,
                GiftBookCoverUrl = w.GiftBookCoverUrl,
                AnnouncedAt = w.AnnouncedAt
            })
            .ToListAsync(cancellationToken);

        if (winners.Count == 0)
        {
            return null;
        }

        var announcedAt = winners.Max(w => w.AnnouncedAt);
        var isActive = DateTime.UtcNow - announcedAt <= AnnouncementWindow;

        return new AnnouncedChallengeDto
        {
            Year = year,
            Month = month,
            PeriodLabel = ChallengeCalendar.PeriodLabel(year, month),
            AnnouncedAt = announcedAt,
            IsAnnouncementActive = isActive,
            Winners = winners
        };
    }
}
