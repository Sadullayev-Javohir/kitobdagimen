using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;

/// <summary>
/// Shared loader for a quote with its comment thread, used by both the id-based
/// (<see cref="GetQuoteByIdQuery"/>) and slug-based lookups.
/// </summary>
internal static class QuoteDetailLoader
{
    public static async Task<QuoteDetailDto> LoadAsync(
        IQueryable<Domain.Entities.Quote> source,
        int? currentUserId,
        string? viewerEmail,
        CancellationToken cancellationToken)
    {
        var quote = await source
            .ToQuoteDto(currentUserId, viewerEmail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Iqtibos topilmadi.");

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
