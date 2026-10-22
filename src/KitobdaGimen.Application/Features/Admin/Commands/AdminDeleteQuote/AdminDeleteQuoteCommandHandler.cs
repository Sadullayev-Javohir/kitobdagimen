using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteQuote;

public class AdminDeleteQuoteCommandHandler : IRequestHandler<AdminDeleteQuoteCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITelegramBotService _telegramBot;

    public AdminDeleteQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITelegramBotService telegramBot)
    {
        _db = db;
        _currentUser = currentUser;
        _telegramBot = telegramBot;
    }

    public async Task<Unit> Handle(AdminDeleteQuoteCommand request, CancellationToken cancellationToken)
    {
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.Admin, cancellationToken);

        var quote = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken)
            ?? throw new NotFoundException("Iqtibos", request.QuoteId);

        if (quote.TelegramChatId.HasValue && quote.TelegramMessageId.HasValue)
        {
            _ = _telegramBot.DeleteMessageAsync(quote.TelegramChatId.Value, quote.TelegramMessageId.Value, CancellationToken.None);
        }

        // SavedQuote rows cascade from the quote at the DB.
        _db.Quotes.Remove(quote);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
