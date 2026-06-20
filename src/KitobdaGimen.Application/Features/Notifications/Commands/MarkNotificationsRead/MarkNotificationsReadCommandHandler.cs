using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsRead;

public class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationsReadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var query = _db.Notifications.Where(n => n.RecipientId == userId && !n.IsRead);
        if (request.Ids is { Count: > 0 })
        {
            query = query.Where(n => request.Ids.Contains(n.Id));
        }

        var toMark = await query.ToListAsync(cancellationToken);
        if (toMark.Count == 0) return Unit.Value;

        foreach (var n in toMark) n.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
