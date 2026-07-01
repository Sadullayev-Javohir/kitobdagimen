using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuoteComment;

public class DeleteQuoteCommentCommandHandler : IRequestHandler<DeleteQuoteCommentCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteQuoteCommentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteQuoteCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var comment = await _db.QuoteComments
            .Include(c => c.Quote)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
            ?? throw new NotFoundException("Izoh", request.CommentId);

        // The comment's own author may delete it; the quote owner may delete any
        // comment left on their quote.
        if (comment.UserId != userId && comment.Quote.UserId != userId)
        {
            throw new ForbiddenAccessException("Bu izohni o'chirishga ruxsatingiz yo'q.");
        }

        // Remove this comment's replies first (ParentComment FK is Restrict, so they
        // can't be cascaded by the database). Threading is single-level, so replies
        // themselves have no children.
        var replies = await _db.QuoteComments
            .Where(c => c.ParentCommentId == comment.Id)
            .ToListAsync(cancellationToken);
        if (replies.Count > 0)
        {
            _db.QuoteComments.RemoveRange(replies);
        }

        _db.QuoteComments.Remove(comment);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
