using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Stories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Stories.Queries.GetStoryById;

public class GetStoryByIdQueryHandler : IRequestHandler<GetStoryByIdQuery, StoryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetStoryByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StoryDto> Handle(GetStoryByIdQuery request, CancellationToken cancellationToken)
    {
        // No WhereActive(): the detail page shows the story even after it has expired.
        return await _db.Stories
            .Where(s => s.Id == request.StoryId)
            .ToStoryDto(_currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Story", request.StoryId);
    }
}
