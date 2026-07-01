using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeWins;

public class GetUserChallengeWinsQueryHandler
    : IRequestHandler<GetUserChallengeWinsQuery, IReadOnlyList<UserChallengeWinDto>>
{
    private readonly IAppDbContext _db;

    public GetUserChallengeWinsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserChallengeWinDto>> Handle(
        GetUserChallengeWinsQuery request, CancellationToken cancellationToken)
    {
        var wins = await _db.ChallengeWinners
            .Where(w => w.UserId == request.UserId)
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Month)
            .Select(w => new
            {
                w.Year,
                w.Month,
                w.Rank,
                w.PagesRead,
                w.BooksRead,
                LikeCount = w.Likes.Count,
                w.GiftBookTitle,
                w.GiftBookAuthor,
                w.GiftBookCoverUrl
            })
            .ToListAsync(cancellationToken);

        return wins.Select(w => new UserChallengeWinDto
        {
            Year = w.Year,
            Month = w.Month,
            PeriodLabel = ChallengeCalendar.PeriodLabel(w.Year, w.Month),
            Rank = w.Rank,
            PagesRead = w.PagesRead,
            BooksRead = w.BooksRead,
            LikeCount = w.LikeCount,
            GiftBookTitle = w.GiftBookTitle,
            GiftBookAuthor = w.GiftBookAuthor,
            GiftBookCoverUrl = w.GiftBookCoverUrl
        }).ToList();
    }
}
