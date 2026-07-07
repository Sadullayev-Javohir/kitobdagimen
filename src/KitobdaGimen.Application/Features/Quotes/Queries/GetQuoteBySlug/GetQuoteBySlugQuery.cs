using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteBySlug;

/// <summary>Returns a single quote (by its public slug) with its comment thread, or throws if it does not exist.</summary>
public record GetQuoteBySlugQuery(string Slug) : IRequest<QuoteDetailDto>;
