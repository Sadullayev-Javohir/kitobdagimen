using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteQuote;

/// <summary>Admin/SuperAdmin deletes ANY quote (moderation), regardless of author.</summary>
public record AdminDeleteQuoteCommand(int QuoteId) : IRequest<Unit>;
