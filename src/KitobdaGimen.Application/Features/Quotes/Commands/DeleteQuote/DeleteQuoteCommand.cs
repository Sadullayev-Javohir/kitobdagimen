using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuote;

/// <summary>Deletes a quote owned by the current user.</summary>
public record DeleteQuoteCommand(int QuoteId) : IRequest<Unit>;
