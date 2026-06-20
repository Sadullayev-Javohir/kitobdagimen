using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.UpdatePost;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdatePostCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PostDto> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId);

        if (post.UserId != userId)
        {
            throw new ForbiddenAccessException("Faqat o'z postingizni tahrirlay olasiz.");
        }

        post.ReviewText = RichTextSanitizer.Sanitize(request.ReviewText);
        post.ImageUrl = request.ImageUrl;

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Posts
            .Where(p => p.Id == post.Id)
            .ToPostDto(userId)
            .FirstAsync(cancellationToken);
    }
}
