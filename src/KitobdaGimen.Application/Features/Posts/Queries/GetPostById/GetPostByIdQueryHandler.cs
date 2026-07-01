using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostById;

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPostByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PostDetailDto> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        => PostDetailLoader.LoadAsync(
            _db.Posts.Where(p => p.Id == request.PostId),
            _currentUser.UserId,
            _currentUser.Email?.ToLowerInvariant(),
            cancellationToken);
}
