using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Notifications.Queries.GetUnreadNotifications;

public class GetUnreadNotificationsQueryHandler
    : IRequestHandler<GetUnreadNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    /// <summary>Cap the replay so a long-offline user doesn't pull an unbounded list.</summary>
    private const int MaxItems = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadNotificationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        return await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(MaxItems)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                RelatedId = n.RelatedId,
                ActorId = n.ActorId,
                ActorName = n.ActorName,
                ActorAvatarUrl = n.ActorAvatarUrl,
                Message = n.Message,
                Url = n.Url,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
