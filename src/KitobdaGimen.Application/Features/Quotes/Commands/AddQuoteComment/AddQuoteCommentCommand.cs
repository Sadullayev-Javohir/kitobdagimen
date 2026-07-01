using KitobdaGimen.Application.Features.Posts.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;

/// <summary>Adds a comment (or a one-level reply) to a quote.</summary>
public record AddQuoteCommentCommand(int QuoteId, string Text, int? ParentCommentId = null) : IRequest<CommentDto>;
