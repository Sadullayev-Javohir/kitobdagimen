using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.RecordQuoteView;

public class RecordQuoteViewCommandHandler : IRequestHandler<RecordQuoteViewCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordQuoteViewCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(RecordQuoteViewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // Views are only tracked for authenticated users; silently ignore anonymous reads.
        if (userId is null)
        {
            return await _db.QuoteViews.CountAsync(v => v.QuoteId == request.QuoteId, cancellationToken);
        }

        var alreadyViewed = await _db.QuoteViews
            .AnyAsync(v => v.QuoteId == request.QuoteId && v.UserId == userId, cancellationToken);

        if (!alreadyViewed)
        {
            var quoteExists = await _db.Quotes.AnyAsync(q => q.Id == request.QuoteId, cancellationToken);
            if (quoteExists)
            {
                _db.QuoteViews.Add(new QuoteView
                {
                    QuoteId = request.QuoteId,
                    UserId = userId.Value,
                    ViewedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return await _db.QuoteViews.CountAsync(v => v.QuoteId == request.QuoteId, cancellationToken);
    }
}
