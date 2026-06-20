using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;

/// <summary>Saves a new quote from a book for the current user.</summary>
public record CreateQuoteCommand : IRequest<QuoteDto>
{
    public int BookId { get; init; }
    public string Text { get; init; } = null!;
}
