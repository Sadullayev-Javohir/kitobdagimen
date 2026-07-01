using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Follow.Dtos;
using FollowEntity = KitobdaGimen.Domain.Entities.Follow;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Follow.Commands.ToggleFollow;

public class ToggleFollowCommandHandler : IRequestHandler<ToggleFollowCommand, FollowResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ToggleFollowCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<FollowResultDto> Handle(ToggleFollowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        if (request.TargetUserId == userId)
        {
            throw new ForbiddenAccessException("O'zingizni kuzata olmaysiz.");
        }

        var targetExists = await _db.Users.AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);
        if (!targetExists)
        {
            throw new NotFoundException("Foydalanuvchi", request.TargetUserId);
        }

        var existing = await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowerId == userId && f.FollowingId == request.TargetUserId, cancellationToken);

        bool isFollowing;
        if (existing is null)
        {
            _db.Follows.Add(new FollowEntity
            {
                FollowerId = userId,
                FollowingId = request.TargetUserId,
                CreatedAt = DateTime.UtcNow
            });
            isFollowing = true;
        }
        else
        {
            _db.Follows.Remove(existing);
            isFollowing = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var followerCount = await _db.Follows
            .CountAsync(f => f.FollowingId == request.TargetUserId, cancellationToken);

        // Notify the followed user (only when starting to follow).
        if (isFollowing)
        {
            var actor = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstAsync(cancellationToken);

            await _notifications.NotifyAsync(request.TargetUserId, new NotificationDto
            {
                Type = "follow",
                ActorName = actor.FullName,
                ActorAvatarUrl = AvatarPrivacy.ForActor(_currentUser.Email?.ToLowerInvariant(), actor.AvatarUrl),
                Message = $"{actor.FullName} sizni kuzata boshladi",
                Url = $"/profile/{userId}"
            }, cancellationToken);
        }

        return new FollowResultDto { IsFollowing = isFollowing, FollowerCount = followerCount };
    }
}
