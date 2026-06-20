using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Commands.DeleteComment;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCommentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var comment = await _db.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
            ?? throw new NotFoundException("Izoh", request.CommentId);

        // The comment's own author may delete it; the post owner may delete any
        // comment left on their post.
        if (comment.UserId != userId && comment.Post.UserId != userId)
        {
            throw new ForbiddenAccessException("Bu izohni o'chirishga ruxsatingiz yo'q.");
        }

        // Remove this comment's replies first (ParentComment FK is Restrict, so they
        // can't be cascaded by the database). Threading is single-level, so replies
        // themselves have no children.
        var replies = await _db.Comments
            .Where(c => c.ParentCommentId == comment.Id)
            .ToListAsync(cancellationToken);
        if (replies.Count > 0)
        {
            _db.Comments.RemoveRange(replies);
        }

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
