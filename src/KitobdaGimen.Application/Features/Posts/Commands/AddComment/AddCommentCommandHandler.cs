using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public AddCommentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
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

        if (request.ParentCommentId is int parentId)
        {
            var parent = await _db.Comments
                .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                ?? throw new NotFoundException("Izoh", parentId);

            if (parent.PostId != request.PostId)
            {
                throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(request.ParentCommentId), "Izoh boshqa postga tegishli.")
                });
            }

            // Keep threading to a single level: replies attach to the root comment.
            if (parent.ParentCommentId is int rootId)
            {
                request = request with { ParentCommentId = rootId };
            }
        }

        var comment = new Comment
        {
            PostId = request.PostId,
            UserId = userId,
            Text = request.Text,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Comments
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
                IsPostAuthor = c.UserId == postAuthorId.Value
            })
            .FirstAsync(cancellationToken);

        // Notify the post author of a new comment (skip commenting on your own post).
        if (postAuthorId.Value != userId)
        {
            await _notifications.NotifyAsync(postAuthorId.Value, new NotificationDto
            {
                Type = "comment",
                ActorName = dto.Author.FullName,
                ActorAvatarUrl = dto.Author.AvatarUrl,
                Message = $"{dto.Author.FullName} postingizga izoh qoldirdi",
                Url = $"/posts/{request.PostId}"
            }, cancellationToken);
        }

        return dto;
    }
}
