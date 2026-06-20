using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteReadingGoal;

public class DeleteReadingGoalCommandHandler : IRequestHandler<DeleteReadingGoalCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteReadingGoalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReadingGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var goal = await _db.ReadingGoals
            .FirstOrDefaultAsync(g => g.Id == request.ReadingGoalId, cancellationToken)
            ?? throw new NotFoundException("O'qish maqsadi", request.ReadingGoalId);

        if (goal.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        _db.ReadingGoals.Remove(goal);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
