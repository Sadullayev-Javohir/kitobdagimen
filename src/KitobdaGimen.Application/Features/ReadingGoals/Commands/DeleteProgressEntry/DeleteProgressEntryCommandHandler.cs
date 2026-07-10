using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteProgressEntry;

public class DeleteProgressEntryCommandHandler : IRequestHandler<DeleteProgressEntryCommand, ReadingGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteProgressEntryCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReadingGoalDto> Handle(DeleteProgressEntryCommand request, CancellationToken cancellationToken)
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

        // Ko'rsatilgan kundagi barcha yozuvlarni o'chiramiz (bir kunda bir nechta bo'lishi mumkin).
        var entries = await _db.ReadingProgress
            .Where(p => p.ReadingGoalId == goal.Id && p.Date == request.Date)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            throw new NotFoundException("O'qish yozuvi", request.Date);
        }

        _db.ReadingProgress.RemoveRange(entries);

        // Joriy betni saqlanib qolgan yozuvlar yig'indisidan qayta hisoblaymiz — bu
        // avvalgi "tugatildi" clamp'ini ham to'g'rilaydi (CurrentPage = yozuvlar yig'indisi).
        var totalRead = await _db.ReadingProgress
            .Where(p => p.ReadingGoalId == goal.Id && p.Date != request.Date)
            .SumAsync(p => (int?)p.PagesReadToday, cancellationToken) ?? 0;

        var totalPages = goal.Book.TotalPages;
        goal.CurrentPage = totalPages > 0 ? Math.Min(totalRead, totalPages) : totalRead;
        goal.IsActive = totalPages <= 0 || goal.CurrentPage < totalPages;

        await _db.SaveChangesAsync(cancellationToken);

        var today = KitobdaGimen.Application.Common.UzTime.Today;
        return await _db.ReadingGoals
            .Where(g => g.Id == goal.Id)
            .ToReadingGoalDto(today)
            .FirstAsync(cancellationToken);
    }
}
