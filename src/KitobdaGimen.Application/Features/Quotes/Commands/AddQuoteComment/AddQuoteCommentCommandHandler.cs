using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;

public class AddQuoteCommentCommandHandler : IRequestHandler<AddQuoteCommentCommand, CommentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public AddQuoteCommentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<CommentDto> Handle(AddQuoteCommentCommand request, CancellationToken cancellationToken)
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

        var parentCommentId = request.ParentCommentId;
        if (parentCommentId is int parentId)
        {
            var parent = await _db.QuoteComments
                .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                ?? throw new NotFoundException("Izoh", parentId);

            if (parent.QuoteId != request.QuoteId)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(request.ParentCommentId), "Izoh boshqa iqtibosga tegishli.")
                });
            }

            // Keep threading to a single level: replies attach to the root comment.
            if (parent.ParentCommentId is int rootId)
            {
                parentCommentId = rootId;
            }
        }

        var comment = new QuoteComment
        {
            QuoteId = request.QuoteId,
            UserId = userId,
            Text = request.Text,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _db.QuoteComments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.QuoteComments
            .Where(c => c.Id == comment.Id)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                ParentCommentId = c.ParentCommentId,
                Author = new UserSummaryDto
                {
                    Id = c.User.Id,
                    Username = c.User.Username,
                    FullName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl
                },
                IsPostAuthor = c.UserId == quoteAuthorId.Value
            })
            .FirstAsync(cancellationToken);

        // Cheklangan foydalanuvchi izoh yozsa, uning avatari boshqalarga ko'rsatilmasin.
        var actorEmail = _currentUser.Email?.ToLowerInvariant();
        dto = dto with
        {
            Author = dto.Author with
            {
                AvatarUrl = AvatarPrivacy.ForActor(actorEmail, dto.Author.AvatarUrl)
            }
        };

        // Notify the quote author of a new comment (skip commenting on your own quote).
        if (quoteAuthorId.Value != userId)
        {
            await _notifications.NotifyAsync(quoteAuthorId.Value, new NotificationDto
            {
                Type = "comment",
                ActorId = userId,
                ActorName = dto.Author.FullName,
                ActorAvatarUrl = dto.Author.AvatarUrl,
                Message = $"{dto.Author.FullName} iqtibosingizga izoh qoldirdi",
                Url = $"/quotes/{request.QuoteId}"
            }, cancellationToken);
        }

        return dto;
    }
}
