using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Admin;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;

public class FinalizeChallengeMonthCommandHandler
    : IRequestHandler<FinalizeChallengeMonthCommand, int>
{
    /// <summary>E'lon qilinadigan g'oliblar soni: 1-, 2- va 3-o'rin.</summary>
    private const int WinnerCount = 3;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _mediator;

    public FinalizeChallengeMonthCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, ISender mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<int> Handle(FinalizeChallengeMonthCommand request, CancellationToken cancellationToken)
    {
        if (!request.BypassAdminCheck)
        {
            // Qo'lda ishga tushirishni faqat super admin bajaradi (avtomatik job to'xtab qolsa).
            await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);
        }

        // Idempotent: allaqachon e'lon qilingan bo'lsa qayta yaratmaymiz.
        var already = await _db.ChallengeWinners
            .AnyAsync(w => w.Year == request.Year && w.Month == request.Month, cancellationToken);
        if (already)
        {
            return 0;
        }

        var standings = await _mediator.Send(new GetChallengeStandingsQuery
        {
            Year = request.Year,
            Month = request.Month,
            Limit = WinnerCount
        }, cancellationToken);

        if (standings.Count == 0)
        {
            return 0;
        }

        var announcedAt = DateTime.UtcNow;
        foreach (var s in standings)
        {
            _db.ChallengeWinners.Add(new ChallengeWinner
            {
                Year = request.Year,
                Month = request.Month,
                UserId = s.UserId,
                Rank = s.Rank,
                PagesRead = s.PagesRead,
                BooksRead = s.BooksRead,
                ActiveDays = s.ActiveDays,
                AvgPagesPerDay = s.AvgPagesPerDay,
                AnnouncedAt = announcedAt
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return standings.Count;
    }
}
