using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.ToggleQuoteLike;

/// <summary>Toggles the current user's like on a quote, returning the new state and total.</summary>
public record ToggleQuoteLikeCommand(int QuoteId) : IRequest<LikeResultDto>;
