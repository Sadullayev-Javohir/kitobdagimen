using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Queries.GetFinishedReadingGoals;

public class GetFinishedReadingGoalsQueryHandler
    : IRequestHandler<GetFinishedReadingGoalsQuery, IReadOnlyList<ReadingGoalDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFinishedReadingGoalsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ReadingGoalDto>> Handle(
        GetFinishedReadingGoalsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId
            ?? _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.ReadingGoals
            .Where(g => g.UserId == userId && !g.IsActive)
            .OrderByDescending(g => g.StartDate)
            .ToReadingGoalDto(today)
            .ToListAsync(cancellationToken);
    }
}
