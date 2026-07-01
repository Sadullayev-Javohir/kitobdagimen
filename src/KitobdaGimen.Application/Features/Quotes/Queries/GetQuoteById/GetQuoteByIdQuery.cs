using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;

/// <summary>Loads a single quote with its comment thread for the detail page.</summary>
public record GetQuoteByIdQuery(int QuoteId) : IRequest<QuoteDetailDto>;
