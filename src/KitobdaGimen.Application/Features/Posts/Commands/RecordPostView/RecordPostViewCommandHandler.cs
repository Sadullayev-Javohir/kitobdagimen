using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.RecordPostView;

public class RecordPostViewCommandHandler : IRequestHandler<RecordPostViewCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordPostViewCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RecordPostViewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // Views are only tracked for authenticated users; silently ignore anonymous reads.
        if (userId is null)
        {
            return Unit.Value;
        }

        var alreadyViewed = await _db.PostViews
            .AnyAsync(v => v.PostId == request.PostId && v.UserId == userId, cancellationToken);

        if (alreadyViewed)
        {
            return Unit.Value;
        }

        var postExists = await _db.Posts.AnyAsync(p => p.Id == request.PostId, cancellationToken);
        if (!postExists)
        {
            return Unit.Value;
        }

        _db.PostViews.Add(new PostView
        {
            PostId = request.PostId,
            UserId = userId.Value,
            ViewedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
