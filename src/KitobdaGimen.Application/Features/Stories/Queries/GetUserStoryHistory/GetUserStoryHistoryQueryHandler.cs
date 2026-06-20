using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetUserStoryHistory;

public class GetUserStoryHistoryQueryHandler : IRequestHandler<GetUserStoryHistoryQuery, IReadOnlyList<StoryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserStoryHistoryQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StoryDto>> Handle(GetUserStoryHistoryQuery request, CancellationToken cancellationToken)
    {
        // No WhereActive() here: the profile lists the user's whole story history, expired or not.
        return await _db.Stories
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToStoryDto(_currentUser.UserId)
            .ToListAsync(cancellationToken);
    }
}
