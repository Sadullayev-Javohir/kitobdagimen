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

        if (user.Role == UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Super adminni o'chirib bo'lmaydi.");
        }

        var userId = user.Id;

        // 1. Stories: likes/views/stories
        _db.StoryLikes.RemoveRange(await _db.StoryLikes
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.StoryViews.RemoveRange(await _db.StoryViews
            .Where(x => x.UserId == userId || x.Story.UserId == userId).ToListAsync(cancellationToken));
        _db.Stories.RemoveRange(await _db.Stories
            .Where(s => s.UserId == userId).ToListAsync(cancellationToken));

        // 2. Posts & engagement: likes, views, comments
        _db.Likes.RemoveRange(await _db.Likes
            .Where(l => l.UserId == userId || l.Post.UserId == userId).ToListAsync(cancellationToken));
        _db.PostViews.RemoveRange(await _db.PostViews
            .Where(v => v.UserId == userId || v.Post.UserId == userId).ToListAsync(cancellationToken));
        _db.Comments.RemoveRange(await _db.Comments
            .Where(c => c.UserId == userId || c.Post.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));

        // 3. Messages & Conversations
        _db.MessageReactions.RemoveRange(await _db.MessageReactions
            .Where(r => r.UserId == userId).ToListAsync(cancellationToken));
        _db.Messages.RemoveRange(await _db.Messages
            .Where(m => m.SenderId == userId).ToListAsync(cancellationToken));
        _db.Conversations.RemoveRange(await _db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId).ToListAsync(cancellationToken));

        // 4. Social: Follows, Connections, Notifications
        _db.Follows.RemoveRange(await _db.Follows
            .Where(f => f.FollowerId == userId || f.FollowingId == userId).ToListAsync(cancellationToken));
        _db.Connections.RemoveRange(await _db.Connections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId).ToListAsync(cancellationToken));
        _db.Notifications.RemoveRange(await _db.Notifications
            .Where(n => n.RecipientId == userId || n.ActorId == userId).ToListAsync(cancellationToken));

        // 5. Quotes & engagement: saved, likes, comments, views
        _db.SavedQuotes.RemoveRange(await _db.SavedQuotes
            .Where(sq => sq.UserId == userId).ToListAsync(cancellationToken));
        _db.QuoteLikes.RemoveRange(await _db.QuoteLikes
            .Where(l => l.UserId == userId || l.Quote.UserId == userId).ToListAsync(cancellationToken));
        _db.QuoteComments.RemoveRange(await _db.QuoteComments
            .Where(c => c.UserId == userId || c.Quote.UserId == userId ||
                        (c.ParentCommentId != null && c.ParentComment!.UserId == userId))
            .ToListAsync(cancellationToken));
        _db.QuoteViews.RemoveRange(await _db.QuoteViews
            .Where(v => v.UserId == userId || v.Quote.UserId == userId).ToListAsync(cancellationToken));

        // 6. Physical Books & Reservations
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

        // 7. Challenge Winners & Likes
        _db.ChallengeWinnerLikes.RemoveRange(await _db.ChallengeWinnerLikes
            .Where(l => l.UserId == userId).ToListAsync(cancellationToken));
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

        // 8. Push subscriptions, Reading Goals & Progress, User Genres
        _db.PushSubscriptions.RemoveRange(await _db.PushSubscriptions
            .Where(ps => ps.UserId == userId).ToListAsync(cancellationToken));

        _db.ReadingProgress.RemoveRange(await _db.ReadingProgress
            .Where(rp => rp.ReadingGoal.UserId == userId).ToListAsync(cancellationToken));
        _db.ReadingGoals.RemoveRange(await _db.ReadingGoals
            .Where(rg => rg.UserId == userId).ToListAsync(cancellationToken));

        _db.UserGenres.RemoveRange(await _db.UserGenres
            .Where(ug => ug.UserId == userId).ToListAsync(cancellationToken));

        // 9. Posts & Quotes
        _db.Posts.RemoveRange(await _db.Posts.Where(p => p.UserId == userId).ToListAsync(cancellationToken));
        _db.Quotes.RemoveRange(await _db.Quotes.Where(q => q.UserId == userId).ToListAsync(cancellationToken));

        // 10. AppSettings (Telegram Username mapping)
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
