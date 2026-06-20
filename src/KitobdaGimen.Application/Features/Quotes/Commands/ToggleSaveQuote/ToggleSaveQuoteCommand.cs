using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.ToggleSaveQuote;

/// <summary>Saves the quote to the current user's collection, or removes it if already saved.</summary>
public record ToggleSaveQuoteCommand(int QuoteId) : IRequest<SaveQuoteResultDto>;
