using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, QuoteDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetQuoteByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<QuoteDetailDto> Handle(GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId;
        var viewerEmail = _currentUser.Email?.ToLowerInvariant();

        var source = _db.Quotes.Where(q => q.Id == request.QuoteId);

        var quote = await source
            .ToQuoteDto(currentUserId, viewerEmail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Iqtibos", request.QuoteId);

        // Load all comments flat, then build the parent/reply tree in memory.
        var flat = await source
            .SelectMany(q => q.Comments)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                ParentCommentId = c.ParentCommentId,
                Author = new UserSummaryDto
                {
                    Id = c.User.Id,
                    Username = c.User.Username,
                    FullName = c.User.FullName,
                    AvatarUrl = (c.User.Email.ToLower() == AvatarPrivacy.RestrictedEmail
                                 && viewerEmail != AvatarPrivacy.AllowedViewerEmail
                                 && viewerEmail != AvatarPrivacy.RestrictedEmail)
                        ? null
                        : c.User.AvatarUrl
                },
                IsPostAuthor = c.UserId == quote.Author.Id
            })
            .ToListAsync(cancellationToken);

        var repliesByParent = flat
            .Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CommentDto>)g.ToList());

        var topLevel = flat
            .Where(c => c.ParentCommentId is null)
            .Select(c => c with
            {
                Replies = repliesByParent.TryGetValue(c.Id, out var replies)
                    ? replies
                    : Array.Empty<CommentDto>()
            })
            .ToList();

        return new QuoteDetailDto
        {
            Quote = quote,
            Comments = topLevel
        };
    }
}
