using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Posts.Queries.GetPostById;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostBySlug;

public class GetPostBySlugQueryHandler : IRequestHandler<GetPostBySlugQuery, PostDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPostBySlugQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PostDetailDto> Handle(GetPostBySlugQuery request, CancellationToken cancellationToken)
        => PostDetailLoader.LoadAsync(
            _db.Posts.Where(p => p.Slug == request.Slug),
            _currentUser.UserId,
            cancellationToken);
}
