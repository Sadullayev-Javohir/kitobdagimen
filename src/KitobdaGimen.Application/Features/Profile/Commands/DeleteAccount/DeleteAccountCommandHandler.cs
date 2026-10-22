using FluentValidation.Results;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
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

        // Akkauntni faqat super admin o'chira oladi — oddiy foydalanuvchi o'z profilini o'chira olmaydi.
        if (user.Role != Domain.Enums.UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Akkauntni faqat super admin o'chira oladi.");
        }

        var typed = (request.Email ?? string.Empty).Trim();
        if (!string.Equals(typed, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Email),
                    "Email akkauntingizdagi email bilan mos kelmadi.")
            });
        }

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

        // Message reactions left by this user (Restrict FK).
        _db.MessageReactions.RemoveRange(await _db.MessageReactions
            .Where(r => r.UserId == userId).ToListAsync(cancellationToken));

        // Messages sent by this user
        _db.Messages.RemoveRange(await _db.Messages
            .Where(m => m.SenderId == userId).ToListAsync(cancellationToken));

        // Conversations the user takes part in
        _db.Conversations.RemoveRange(await _db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId).ToListAsync(cancellationToken));

        // Follows in either direction.
        _db.Follows.RemoveRange(await _db.Follows
            .Where(f => f.FollowerId == userId || f.FollowingId == userId).ToListAsync(cancellationToken));

        // Chat connections (invites) in either direction
        _db.Connections.RemoveRange(await _db.Connections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId).ToListAsync(cancellationToken));

        // Notifications addressed to the user or where the user is the actor
        _db.Notifications.RemoveRange(await _db.Notifications
            .Where(n => n.RecipientId == userId || n.ActorId == userId).ToListAsync(cancellationToken));

        // Quotes saved by this user
        _db.SavedQuotes.RemoveRange(await _db.SavedQuotes
            .Where(sq => sq.UserId == userId).ToListAsync(cancellationToken));

        // Quote likes
        _db.QuoteLikes.RemoveRange(await _db.QuoteLikes
            .Where(l => l.UserId == userId || l.Quote.UserId == userId).ToListAsync(cancellationToken));

        // Quote comments
        _db.QuoteComments.RemoveRange(await _db.QuoteComments
            .Where(c => c.UserId == userId || c.Quote.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));

        // Quote views
        _db.QuoteViews.RemoveRange(await _db.QuoteViews
            .Where(v => v.UserId == userId || v.Quote.UserId == userId).ToListAsync(cancellationToken));

        // Physical Books & Reservations
        var reservedByThisUser = await _db.PhysicalBookReservations
            .Include(r => r.PhysicalBook)
            .Where(r => r.ReserverId == userId && r.PhysicalBook.OwnerId != userId)
            .ToListAsync(cancellationToken);
        foreach (var res in reservedByThisUser)
        {
            if (res.PhysicalBook != null && res.PhysicalBook.Status == PhysicalBookStatus.BandQilindi)
            {
                res.PhysicalBook.Status = PhysicalBookStatus.Mavjud;
            }
        }
        _db.PhysicalBookReservations.RemoveRange(await _db.PhysicalBookReservations
            .Where(r => r.ReserverId == userId || r.PhysicalBook.OwnerId == userId).ToListAsync(cancellationToken));
        _db.PhysicalBooks.RemoveRange(await _db.PhysicalBooks
            .Where(b => b.OwnerId == userId).ToListAsync(cancellationToken));

        // Challenge winner likes
        _db.ChallengeWinnerLikes.RemoveRange(await _db.ChallengeWinnerLikes
            .Where(l => l.UserId == userId).ToListAsync(cancellationToken));

        // Challenge winners
        _db.ChallengeWinners.RemoveRange(await _db.ChallengeWinners
            .Where(w => w.UserId == userId).ToListAsync(cancellationToken));
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

        // Push subscriptions, Reading Goals & Progress, User Genres
        _db.PushSubscriptions.RemoveRange(await _db.PushSubscriptions
            .Where(ps => ps.UserId == userId).ToListAsync(cancellationToken));

        _db.ReadingProgress.RemoveRange(await _db.ReadingProgress
            .Where(rp => rp.ReadingGoal.UserId == userId).ToListAsync(cancellationToken));
        _db.ReadingGoals.RemoveRange(await _db.ReadingGoals
            .Where(rg => rg.UserId == userId).ToListAsync(cancellationToken));

        _db.UserGenres.RemoveRange(await _db.UserGenres
            .Where(ug => ug.UserId == userId).ToListAsync(cancellationToken));

        // Owned content
        _db.Posts.RemoveRange(await _db.Posts.Where(p => p.UserId == userId).ToListAsync(cancellationToken));
        _db.Quotes.RemoveRange(await _db.Quotes.Where(q => q.UserId == userId).ToListAsync(cancellationToken));

        // AppSettings (Telegram Username mapping)
        var tgSetting = await _db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == $"Telegram:Username:{userId}", cancellationToken);
        if (tgSetting != null)
        {
            _db.AppSettings.Remove(tgSetting);
        }

        _db.Users.Remove(user);

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
