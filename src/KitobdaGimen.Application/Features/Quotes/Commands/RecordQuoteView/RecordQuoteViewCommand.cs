using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.RecordQuoteView;

/// <summary>
/// Records that the current user viewed a quote and returns the total view count.
/// Idempotent — one view per user per quote (repeat calls just return the current count).
/// </summary>
public record RecordQuoteViewCommand(int QuoteId) : IRequest<int>;
