using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;

public class ToggleSaveQuoteCommandHandler : IRequestHandler<ToggleSaveQuoteCommand, SaveQuoteResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleSaveQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SaveQuoteResultDto> Handle(ToggleSaveQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var quoteExists = await _db.Quotes.AnyAsync(q => q.Id == request.QuoteId, cancellationToken);
        if (!quoteExists)
        {
            throw new NotFoundException("Iqtibos", request.QuoteId);
        }

        var existing = await _db.SavedQuotes
            .FirstOrDefaultAsync(s => s.QuoteId == request.QuoteId && s.UserId == userId, cancellationToken);

        bool isSaved;
        if (existing is null)
        {
            _db.SavedQuotes.Add(new SavedQuote
            {
                QuoteId = request.QuoteId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            isSaved = true;
        }
        else
        {
            _db.SavedQuotes.Remove(existing);
            isSaved = false;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var saveCount = await _db.SavedQuotes.CountAsync(s => s.QuoteId == request.QuoteId, cancellationToken);

        return new SaveQuoteResultDto { IsSaved = isSaved, SaveCount = saveCount };
    }
}
