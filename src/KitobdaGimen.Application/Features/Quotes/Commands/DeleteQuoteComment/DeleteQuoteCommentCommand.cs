using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Commands.DeleteQuoteComment;

/// <summary>Deletes a quote comment (its own author or the quote owner may delete it).</summary>
public record DeleteQuoteCommentCommand(int CommentId) : IRequest<Unit>;
