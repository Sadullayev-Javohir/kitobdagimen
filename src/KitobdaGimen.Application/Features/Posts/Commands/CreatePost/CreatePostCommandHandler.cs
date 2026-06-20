using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.CreatePost;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public CreatePostCommandHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<PostDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var bookExists = await _db.Books.AnyAsync(b => b.Id == request.BookId, cancellationToken);
        if (!bookExists)
        {
            throw new NotFoundException("Kitob", request.BookId);
        }

        // Random public slug; retry on the rare chance of a collision.
        string slug;
        do
        {
            slug = SlugGenerator.Generate();
        }
        while (await _db.Posts.AnyAsync(p => p.Slug == slug, cancellationToken));

        var post = new Post
        {
            UserId = userId,
            BookId = request.BookId,
            Slug = slug,
            ReviewText = RichTextSanitizer.Sanitize(request.ReviewText),
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.Posts
            .Where(p => p.Id == post.Id)
            .ToPostDto(userId)
            .FirstAsync(cancellationToken);

        // Notify the author's followers that a new review was published.
        var followerIds = await _db.Follows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);
        if (followerIds.Count > 0)
        {
            var username = dto.Author.Username;
            var url = $"/post/{(string.IsNullOrWhiteSpace(username) ? dto.Author.Id.ToString() : username)}/{dto.Slug}";
            await _notifications.NotifyManyAsync(followerIds, new NotificationDto
            {
                Type = "post",
                ActorId = dto.Author.Id,
                ActorName = dto.Author.FullName,
                ActorAvatarUrl = dto.Author.AvatarUrl,
                Message = $"{dto.Author.FullName} yangi post chop etdi",
                Url = url
            }, cancellationToken);
        }

        return dto;
    }
}
