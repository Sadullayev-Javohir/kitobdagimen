using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.CreateReadingGoal;

public class CreateReadingGoalCommandHandler : IRequestHandler<CreateReadingGoalCommand, ReadingGoalDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateReadingGoalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReadingGoalDto> Handle(CreateReadingGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var bookExists = await _db.Books.AnyAsync(b => b.Id == request.BookId, cancellationToken);
        if (!bookExists)
        {
            throw new NotFoundException("Kitob", request.BookId);
        }

        // Deactivate any existing active goal for the same book to keep one active per book.
        var existingActive = await _db.ReadingGoals
            .Where(g => g.UserId == userId && g.BookId == request.BookId && g.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var g in existingActive)
        {
            g.IsActive = false;
        }

        // Normalise the (possibly form-bound, Kind=Unspecified) date to a UTC midnight
        // so Npgsql can persist it to the timestamptz column.
        var startDate = request.StartDate.HasValue
            ? DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc)
            : DateTime.UtcNow.Date;

        var goal = new ReadingGoal
        {
            UserId = userId,
            BookId = request.BookId,
            DailyPageGoal = request.DailyPageGoal,
            StartDate = startDate,
            CurrentPage = 0,
            IsActive = true
        };

        _db.ReadingGoals.Add(goal);
        await _db.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.ReadingGoals
            .Where(g => g.Id == goal.Id)
            .ToReadingGoalDto(today)
            .FirstAsync(cancellationToken);
    }
}
