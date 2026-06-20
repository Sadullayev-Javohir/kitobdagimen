using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Commands.DeleteStory;

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStoryCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var story = await _db.Stories.FirstOrDefaultAsync(s => s.Id == request.StoryId, cancellationToken)
            ?? throw new NotFoundException("Story", request.StoryId);

        if (story.UserId != userId)
        {
            throw new ForbiddenAccessException("Bu story sizga tegishli emas.");
        }

        _db.Stories.Remove(story);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
