using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;

public class ToggleQuoteLikeCommandHandler : IRequestHandler<ToggleQuoteLikeCommand, LikeResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ToggleQuoteLikeCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<LikeResultDto> Handle(ToggleQuoteLikeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var quoteAuthorId = await _db.Quotes
            .Where(q => q.Id == request.QuoteId)
            .Select(q => (int?)q.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (quoteAuthorId is null)
        {
            throw new NotFoundException("Iqtibos", request.QuoteId);
        }

        var existing = await _db.QuoteLikes
            .FirstOrDefaultAsync(l => l.QuoteId == request.QuoteId && l.UserId == userId, cancellationToken);

        bool isLiked;
        if (existing is null)
        {
            _db.QuoteLikes.Add(new QuoteLike
            {
                QuoteId = request.QuoteId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            isLiked = true;
        }
        else
        {
            _db.QuoteLikes.Remove(existing);
            isLiked = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var likeCount = await _db.QuoteLikes.CountAsync(l => l.QuoteId == request.QuoteId, cancellationToken);

        // Notify the quote author of a new like (skip un-likes and liking your own quote).
        if (isLiked && quoteAuthorId.Value != userId)
        {
            var actor = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstAsync(cancellationToken);

            await _notifications.NotifyAsync(quoteAuthorId.Value, new NotificationDto
            {
                Type = "like",
                ActorId = userId,
                ActorName = actor.FullName,
                ActorAvatarUrl = AvatarPrivacy.ForActor(_currentUser.Email?.ToLowerInvariant(), actor.AvatarUrl),
                Message = $"{actor.FullName} iqtibosingizni yoqtirdi",
                Url = $"/quotes/{request.QuoteId}"
            }, cancellationToken);
        }

        return new LikeResultDto { IsLiked = isLiked, LikeCount = likeCount };
    }
}
