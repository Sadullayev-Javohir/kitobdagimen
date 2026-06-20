using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.ToggleLike;

public class ToggleLikeCommandHandler : IRequestHandler<ToggleLikeCommand, LikeResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ToggleLikeCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<LikeResultDto> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var postAuthorId = await _db.Posts
            .Where(p => p.Id == request.PostId)
            .Select(p => (int?)p.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (postAuthorId is null)
        {
            throw new NotFoundException("Post", request.PostId);
        }

        var existing = await _db.Likes
            .FirstOrDefaultAsync(l => l.PostId == request.PostId && l.UserId == userId, cancellationToken);

        bool isLiked;
        if (existing is null)
        {
            _db.Likes.Add(new Like
            {
                PostId = request.PostId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            isLiked = true;
        }
        else
        {
            _db.Likes.Remove(existing);
            isLiked = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var likeCount = await _db.Likes.CountAsync(l => l.PostId == request.PostId, cancellationToken);

        // Notify the post author of a new like (skip un-likes and liking your own post).
        if (isLiked && postAuthorId.Value != userId)
        {
            var actor = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstAsync(cancellationToken);

            await _notifications.NotifyAsync(postAuthorId.Value, new NotificationDto
            {
                Type = "like",
                ActorName = actor.FullName,
                ActorAvatarUrl = actor.AvatarUrl,
                Message = $"{actor.FullName} postingizni yoqtirdi",
                Url = $"/posts/{request.PostId}"
            }, cancellationToken);
        }

        return new LikeResultDto { IsLiked = isLiked, LikeCount = likeCount };
    }
}
