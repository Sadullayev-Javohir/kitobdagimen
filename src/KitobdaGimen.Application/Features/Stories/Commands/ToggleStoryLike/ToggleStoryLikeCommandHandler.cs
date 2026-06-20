using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Stories.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Commands.ToggleStoryLike;

public class ToggleStoryLikeCommandHandler : IRequestHandler<ToggleStoryLikeCommand, StoryLikeResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleStoryLikeCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StoryLikeResultDto> Handle(ToggleStoryLikeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var storyExists = await _db.Stories.AnyAsync(s => s.Id == request.StoryId, cancellationToken);
        if (!storyExists)
        {
            throw new NotFoundException("Story", request.StoryId);
        }

        var existing = await _db.StoryLikes
            .FirstOrDefaultAsync(l => l.StoryId == request.StoryId && l.UserId == userId, cancellationToken);

        bool isLiked;
        if (existing is null)
        {
            _db.StoryLikes.Add(new StoryLike
            {
                StoryId = request.StoryId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            isLiked = true;
        }
        else
        {
            _db.StoryLikes.Remove(existing);
            isLiked = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var likeCount = await _db.StoryLikes.CountAsync(l => l.StoryId == request.StoryId, cancellationToken);

        return new StoryLikeResultDto { IsLiked = isLiked, LikeCount = likeCount };
    }
}
