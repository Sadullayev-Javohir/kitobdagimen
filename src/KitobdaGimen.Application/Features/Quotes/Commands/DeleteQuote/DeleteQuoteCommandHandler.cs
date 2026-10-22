using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuote;

public class DeleteQuoteCommandHandler : IRequestHandler<DeleteQuoteCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITelegramBotService _telegramBot;

    public DeleteQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITelegramBotService telegramBot)
    {
        _db = db;
        _currentUser = currentUser;
        _telegramBot = telegramBot;
    }

    public async Task<Unit> Handle(DeleteQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var quote = await _db.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken)
            ?? throw new NotFoundException("Iqtibos", request.QuoteId);

        if (quote.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        if (quote.TelegramChatId.HasValue && quote.TelegramMessageId.HasValue)
        {
            _ = _telegramBot.DeleteMessageAsync(quote.TelegramChatId.Value, quote.TelegramMessageId.Value, CancellationToken.None);
        }

        _db.Quotes.Remove(quote);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
