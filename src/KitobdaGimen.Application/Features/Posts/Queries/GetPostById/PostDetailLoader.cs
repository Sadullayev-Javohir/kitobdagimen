using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Posts.Queries.GetPostById;

/// <summary>
/// Shared loader for a post with its comment thread, used by both the id-based
/// (<see cref="GetPostByIdQuery"/>) and slug-based lookups.
/// </summary>
internal static class PostDetailLoader
{
    public static async Task<PostDetailDto> LoadAsync(
        IQueryable<Domain.Entities.Post> source,
        int? currentUserId,
        string? viewerEmail,
        CancellationToken cancellationToken)
    {
        var post = await source
            .ToPostDto(currentUserId, viewerEmail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Post topilmadi.");

        // Load all comments flat, then build the parent/reply tree in memory.
        var flat = await source
            .SelectMany(p => p.Comments)
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
                IsPostAuthor = c.UserId == post.Author.Id
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

        return new PostDetailDto
        {
            Post = post,
            Comments = topLevel
        };
    }
}
