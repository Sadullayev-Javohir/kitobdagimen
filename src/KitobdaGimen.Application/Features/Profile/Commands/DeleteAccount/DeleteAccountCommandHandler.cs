using FluentValidation.Results;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = KitobdaGimen.Application.Common.Exceptions.ValidationException;

namespace KitobdaGimen.Application.Features.Profile.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteAccountCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", userId);

        var typed = (request.Email ?? string.Empty).Trim();
        if (!string.Equals(typed, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Email),
                    "Email akkauntingizdagi email bilan mos kelmadi.")
            });
        }

        // Most FKs into User use DeleteBehavior.Restrict (to avoid multiple cascade paths), so we
        // remove the dependent rows explicitly before the user. We delete via tracked entities and
        // let a single SaveChanges order the deletes; rows that cascade from a parent we delete here
        // (comments on this user's posts, messages in this user's conversations, progress of this
        // user's goals, saves of this user's quotes) are handled by the database.

        // Stories: likes/views first, then the stories.
        _db.StoryLikes.RemoveRange(await _db.StoryLikes
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.StoryViews.RemoveRange(await _db.StoryViews
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.Stories.RemoveRange(await _db.Stories
            .Where(s => s.UserId == userId).ToListAsync(cancellationToken));

        // Post engagement by this user (or on this user's posts).
        _db.Likes.RemoveRange(await _db.Likes
            .Where(l => l.UserId == userId || l.Post.UserId == userId).ToListAsync(cancellationToken));
        _db.PostViews.RemoveRange(await _db.PostViews
            .Where(v => v.UserId == userId || v.Post.UserId == userId).ToListAsync(cancellationToken));

        // Comments authored by the user, on the user's posts, or replying to the user's comments.
        _db.Comments.RemoveRange(await _db.Comments
            .Where(c => c.UserId == userId || c.Post.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));

        // Message reactions left by this user (Restrict FK). Reactions on this user's messages
        // cascade from the conversation delete below, but reactions the user placed elsewhere must
        // be removed explicitly or the User delete is blocked.
        _db.MessageReactions.RemoveRange(await _db.MessageReactions
            .Where(r => r.UserId == userId).ToListAsync(cancellationToken));

        // Conversations the user takes part in (messages + their reactions cascade from the
        // conversation at the DB).
        _db.Conversations.RemoveRange(await _db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId).ToListAsync(cancellationToken));

        // Follows in either direction.
        _db.Follows.RemoveRange(await _db.Follows
            .Where(f => f.FollowerId == userId || f.FollowingId == userId).ToListAsync(cancellationToken));

        // Chat connections (invites) in either direction — Restrict FKs, so must be removed explicitly.
        _db.Connections.RemoveRange(await _db.Connections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId).ToListAsync(cancellationToken));

        // Notifications addressed to the user (cascade FK) or where the user is the actor (no FK).
        _db.Notifications.RemoveRange(await _db.Notifications
            .Where(n => n.RecipientId == userId || n.ActorId == userId).ToListAsync(cancellationToken));

        // Quotes saved by this user (saves on the user's own quotes cascade from the quote).
        _db.SavedQuotes.RemoveRange(await _db.SavedQuotes
            .Where(sq => sq.UserId == userId).ToListAsync(cancellationToken));

        // Quote likes placed by this user (Restrict FK). Likes on the user's own quotes cascade
        // when the quotes are removed; likes on other users' quotes must be removed explicitly.
        _db.QuoteLikes.RemoveRange(await _db.QuoteLikes
            .Where(l => l.UserId == userId).ToListAsync(cancellationToken));

        // Quote comments authored by the user, on the user's quotes, or replying to the user's
        // comments (Restrict FKs — mirror the post-comment cleanup above).
        _db.QuoteComments.RemoveRange(await _db.QuoteComments
            .Where(c => c.UserId == userId || c.Quote.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));

        // Challenge winner likes placed by this user (Restrict FK). Likes on the user's own
        // winner rows cascade from the winner; likes elsewhere must be removed explicitly.
        _db.ChallengeWinnerLikes.RemoveRange(await _db.ChallengeWinnerLikes
            .Where(l => l.UserId == userId).ToListAsync(cancellationToken));

        // Owned content that cascades its own dependents.
        _db.Posts.RemoveRange(await _db.Posts.Where(p => p.UserId == userId).ToListAsync(cancellationToken));
        _db.Quotes.RemoveRange(await _db.Quotes.Where(q => q.UserId == userId).ToListAsync(cancellationToken));
        _db.ReadingGoals.RemoveRange(await _db.ReadingGoals.Where(rg => rg.UserId == userId).ToListAsync(cancellationToken));
        _db.UserGenres.RemoveRange(await _db.UserGenres.Where(ug => ug.UserId == userId).ToListAsync(cancellationToken));

        _db.Users.Remove(user);

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
