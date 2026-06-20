using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Commands.RecordStoryView;

public class RecordStoryViewCommandHandler : IRequestHandler<RecordStoryViewCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordStoryViewCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(RecordStoryViewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var ownerId = await _db.Stories
            .Where(s => s.Id == request.StoryId)
            .Select(s => (int?)s.UserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Story", request.StoryId);

        // Don't count the author viewing their own story; record a single view per other user.
        if (ownerId != userId)
        {
            var alreadyViewed = await _db.StoryViews
                .AnyAsync(v => v.StoryId == request.StoryId && v.UserId == userId, cancellationToken);
            if (!alreadyViewed)
            {
                _db.StoryViews.Add(new StoryView
                {
                    StoryId = request.StoryId,
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return await _db.StoryViews.CountAsync(v => v.StoryId == request.StoryId, cancellationToken);
    }
}
