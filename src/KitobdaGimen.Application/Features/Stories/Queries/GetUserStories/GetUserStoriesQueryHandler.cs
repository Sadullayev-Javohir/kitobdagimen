using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetUserStories;

public class GetUserStoriesQueryHandler : IRequestHandler<GetUserStoriesQuery, IReadOnlyList<StoryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserStoriesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StoryDto>> Handle(GetUserStoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Stories
            .Where(s => s.UserId == request.UserId)
            .WhereActive()
            .OrderBy(s => s.CreatedAt)
            .ToStoryDto(_currentUser.UserId, _currentUser.Email?.ToLowerInvariant())
            .ToListAsync(cancellationToken);
    }
}
