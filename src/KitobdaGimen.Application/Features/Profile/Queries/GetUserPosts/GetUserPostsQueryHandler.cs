using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserPosts;

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery, PagedResult<PostDto>>
{
    private const int MaxPageSize = 50;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserPostsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PostDto>> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var source = _db.Posts.Where(p => p.UserId == request.UserId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToPostDto(_currentUser.UserId)
            .ToListAsync(cancellationToken);

        return PagedResult<PostDto>.Create(items, page, pageSize, totalCount);
    }
}
