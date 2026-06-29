using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsReadByUrl;

public class MarkNotificationsReadByUrlCommandHandler : IRequestHandler<MarkNotificationsReadByUrlCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationsReadByUrlCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkNotificationsReadByUrlCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null || string.IsNullOrWhiteSpace(request.Url))
        {
            return Unit.Value;
        }

        var toMark = await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead && n.Url == request.Url)
            .ToListAsync(cancellationToken);

        if (toMark.Count > 0)
        {
            foreach (var n in toMark) n.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
