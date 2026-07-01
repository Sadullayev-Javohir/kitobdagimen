using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;

public class AdminDeleteUserCommandHandler : IRequestHandler<AdminDeleteUserCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminDeleteUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var (callerId, _) = await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);

        if (request.TargetUserId == callerId)
        {
            throw new ForbiddenAccessException("O'zingizni o'chira olmaysiz.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Foydalanuvchi", request.TargetUserId);

        var userId = user.Id;

        // Dependent rows with Restrict FKs are removed explicitly before the user; rows that cascade
        // from a parent (comments on this user's posts, messages in this user's conversations, etc.)
        // are handled by the database. Mirrors the user-initiated DeleteAccount logic.
        _db.StoryLikes.RemoveRange(await _db.StoryLikes
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.StoryViews.RemoveRange(await _db.StoryViews
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.Stories.RemoveRange(await _db.Stories
            .Where(s => s.UserId == userId).ToListAsync(cancellationToken));

        _db.Likes.RemoveRange(await _db.Likes
            .Where(l => l.UserId == userId || l.Post.UserId == userId).ToListAsync(cancellationToken));
        _db.PostViews.RemoveRange(await _db.PostViews
            .Where(v => v.UserId == userId || v.Post.UserId == userId).ToListAsync(cancellationToken));

        _db.Comments.RemoveRange(await _db.Comments
            .Where(c => c.UserId == userId || c.Post.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));

        // Message reactions left by this user (Restrict FK). Reactions on this user's messages
        // cascade from the conversation delete below, but reactions the user placed elsewhere must
        // be removed explicitly or the User delete is blocked.
        _db.MessageReactions.RemoveRange(await _db.MessageReactions
            .Where(r => r.UserId == userId).ToListAsync(cancellationToken));

        // Messages sent by this user (SenderId has Restrict FK). Must be removed explicitly
        // before deleting conversations, or the User delete is blocked.
        _db.Messages.RemoveRange(await _db.Messages
            .Where(m => m.SenderId == userId).ToListAsync(cancellationToken));

        _db.Conversations.RemoveRange(await _db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId).ToListAsync(cancellationToken));

        _db.Follows.RemoveRange(await _db.Follows
            .Where(f => f.FollowerId == userId || f.FollowingId == userId).ToListAsync(cancellationToken));

        _db.Connections.RemoveRange(await _db.Connections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId).ToListAsync(cancellationToken));

        _db.Notifications.RemoveRange(await _db.Notifications
            .Where(n => n.RecipientId == userId || n.ActorId == userId).ToListAsync(cancellationToken));

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

        // Challenge winner entries for this user (though UserId has Cascade, explicitly remove
        // to avoid multi-path cascade issues and to clear GiftedByUserId references).
        _db.ChallengeWinners.RemoveRange(await _db.ChallengeWinners
            .Where(w => w.UserId == userId).ToListAsync(cancellationToken));

        // Also nullify GiftedByUserId where this user gifted books to other winners.
        var winnersGiftedByUser = await _db.ChallengeWinners
            .Where(w => w.GiftedByUserId == userId).ToListAsync(cancellationToken);
        foreach (var w in winnersGiftedByUser)
        {
            w.GiftedByUserId = null;
            w.GiftedAt = null;
            w.GiftBookTitle = null;
            w.GiftBookAuthor = null;
            w.GiftBookCoverUrl = null;
        }

        _db.PushSubscriptions.RemoveRange(await _db.PushSubscriptions
            .Where(ps => ps.UserId == userId).ToListAsync(cancellationToken));

        _db.Posts.RemoveRange(await _db.Posts.Where(p => p.UserId == userId).ToListAsync(cancellationToken));
        _db.Quotes.RemoveRange(await _db.Quotes.Where(q => q.UserId == userId).ToListAsync(cancellationToken));
        _db.ReadingGoals.RemoveRange(await _db.ReadingGoals.Where(rg => rg.UserId == userId).ToListAsync(cancellationToken));
        _db.UserGenres.RemoveRange(await _db.UserGenres.Where(ug => ug.UserId == userId).ToListAsync(cancellationToken));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
