using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;

public class UpdateReadingProgressCommandHandler : IRequestHandler<UpdateReadingProgressCommand, ReadingGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateReadingProgressCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReadingGoalDto> Handle(UpdateReadingProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var goal = await _db.ReadingGoals
            .Include(g => g.Book)
            .FirstOrDefaultAsync(g => g.Id == request.ReadingGoalId, cancellationToken)
            ?? throw new NotFoundException("O'qish maqsadi", request.ReadingGoalId);

        if (goal.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var progress = await _db.ReadingProgress
            .FirstOrDefaultAsync(p => p.ReadingGoalId == goal.Id && p.Date == today, cancellationToken);

        if (progress is null)
        {
            _db.ReadingProgress.Add(new ReadingProgress
            {
                ReadingGoalId = goal.Id,
                Date = today,
                PagesReadToday = request.PagesRead
            });
        }
        else
        {
            progress.PagesReadToday += request.PagesRead;
        }

        var totalPages = goal.Book.TotalPages;
        goal.CurrentPage += request.PagesRead;
        if (totalPages > 0 && goal.CurrentPage >= totalPages)
        {
            goal.CurrentPage = totalPages;
            goal.IsActive = false; // book finished
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.ReadingGoals
            .Where(g => g.Id == goal.Id)
            .ToReadingGoalDto(today)
            .FirstAsync(cancellationToken);
    }
}
