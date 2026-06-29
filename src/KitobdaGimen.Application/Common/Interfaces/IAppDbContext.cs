using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core database context used by Application handlers.
/// Keeps the Application layer independent of the concrete Infrastructure implementation.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Genre> Genres { get; }
    DbSet<UserGenre> UserGenres { get; }
    DbSet<Book> Books { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostView> PostViews { get; }
    DbSet<Like> Likes { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Follow> Follows { get; }
    DbSet<Connection> Connections { get; }
    DbSet<ReadingGoal> ReadingGoals { get; }
    DbSet<ReadingProgress> ReadingProgress { get; }
    DbSet<Quote> Quotes { get; }
    DbSet<SavedQuote> SavedQuotes { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Story> Stories { get; }
    DbSet<StoryView> StoryViews { get; }
    DbSet<StoryLike> StoryLikes { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
