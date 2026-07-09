using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Support;

/// <summary>
/// Lightweight <see cref="IAppDbContext"/> backed by the EF Core in-memory provider.
/// The test project references only Application + Domain (not Infrastructure), so we
/// re-declare a minimal context here instead of reusing the production AppDbContext.
/// Only the parts handlers depend on are configured (composite keys); the in-memory
/// provider ignores relational concerns like indexes and delete behaviours.
/// </summary>
public class TestDbContext : DbContext, IAppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<UserGenre> UserGenres => Set<UserGenre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostView> PostViews => Set<PostView>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<ReadingGoal> ReadingGoals => Set<ReadingGoal>();
    public DbSet<ReadingProgress> ReadingProgress => Set<ReadingProgress>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteView> QuoteViews => Set<QuoteView>();
    public DbSet<SavedQuote> SavedQuotes => Set<SavedQuote>();
    public DbSet<QuoteLike> QuoteLikes => Set<QuoteLike>();
    public DbSet<QuoteComment> QuoteComments => Set<QuoteComment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();
    public DbSet<StoryLike> StoryLikes => Set<StoryLike>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ChallengeWinner> ChallengeWinners => Set<ChallengeWinner>();
    public DbSet<ChallengeWinnerLike> ChallengeWinnerLikes => Set<ChallengeWinnerLike>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<PhysicalBook> PhysicalBooks => Set<PhysicalBook>();
    public DbSet<PhysicalBookReservation> PhysicalBookReservations => Set<PhysicalBookReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // UserGenre is a join entity with a composite key and no surrogate Id.
        modelBuilder.Entity<UserGenre>().HasKey(ug => new { ug.UserId, ug.GenreId });

        // Follow has two FKs to User — disambiguate the navigations explicitly.
        modelBuilder.Entity<Follow>(e =>
        {
            e.HasOne(f => f.Follower).WithMany(u => u.Following).HasForeignKey(f => f.FollowerId);
            e.HasOne(f => f.Following).WithMany(u => u.Followers).HasForeignKey(f => f.FollowingId);
        });

        // Conversation also has two FKs to User (no inverse collections).
        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasOne(c => c.User1).WithMany().HasForeignKey(c => c.User1Id);
            e.HasOne(c => c.User2).WithMany().HasForeignKey(c => c.User2Id);
        });

        // Connection has two FKs to User — disambiguate the navigations explicitly.
        modelBuilder.Entity<Connection>(e =>
        {
            e.HasOne(c => c.Requester).WithMany(u => u.SentConnections).HasForeignKey(c => c.RequesterId);
            e.HasOne(c => c.Addressee).WithMany(u => u.ReceivedConnections).HasForeignKey(c => c.AddresseeId);
        });

        // PhysicalBook -> User (Owner) and Book, both without inverse collections.
        modelBuilder.Entity<PhysicalBook>(e =>
        {
            e.HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerId);
            e.HasOne(p => p.Book).WithMany().HasForeignKey(p => p.BookId);
        });

        // PhysicalBookReservation -> PhysicalBook (with inverse) and User (Reserver, no inverse).
        modelBuilder.Entity<PhysicalBookReservation>(e =>
        {
            e.HasOne(r => r.PhysicalBook).WithMany(p => p.Reservations).HasForeignKey(r => r.PhysicalBookId);
            e.HasOne(r => r.Reserver).WithMany().HasForeignKey(r => r.ReserverId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
