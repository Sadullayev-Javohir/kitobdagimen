using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Posts.Commands.CreatePost;
using KitobdaGimen.Application.Features.Posts.Commands.DeleteComment;
using KitobdaGimen.Application.Features.Posts.Commands.ToggleLike;
using KitobdaGimen.Application.Features.Posts.Queries.GetFeed;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class PostsHandlerTests : TestBase
{
    private static async Task<(User author, Book book)> SeedAuthorAndBookAsync(TestDbContext db, int userId = 1)
    {
        var user = new User { Id = userId, GoogleId = $"g-{userId}", Email = $"u{userId}@e.com", FullName = $"User {userId}", CreatedAt = DateTime.UtcNow };
        var book = new Book { Id = 1, Title = "O'tkan kunlar", Author = "Abdulla Qodiriy", TotalPages = 300 };
        db.Users.Add(user);
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return (user, book);
    }

    [Fact]
    public async Task CreatePost_persists_and_projects_post()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new CreatePostCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        var dto = await handler.Handle(new CreatePostCommand { BookId = 1, ReviewText = "Ajoyib kitob" }, CancellationToken.None);

        Assert.Equal("Ajoyib kitob", dto.ReviewText);
        Assert.Equal(1, dto.Author.Id);
        Assert.Equal("O'tkan kunlar", dto.Book.Title);
        Assert.Equal(0, dto.LikeCount);
        Assert.Single(await db.Posts.ToListAsync());
    }

    [Fact]
    public async Task CreatePost_notifies_each_follower()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db, userId: 1);
        // Two followers of the author, plus a third user who does not follow.
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "User 2", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new User { Id = 3, GoogleId = "g-3", Email = "u3@e.com", FullName = "User 3", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new User { Id = 4, GoogleId = "g-4", Email = "u4@e.com", FullName = "User 4", CreatedAt = DateTime.UtcNow });
        db.Follows.Add(new Follow { FollowerId = 2, FollowingId = 1, CreatedAt = DateTime.UtcNow });
        db.Follows.Add(new Follow { FollowerId = 3, FollowingId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var notifier = new SpyNotificationService();
        var handler = new CreatePostCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        await handler.Handle(new CreatePostCommand { BookId = 1, ReviewText = "Ajoyib kitob" }, CancellationToken.None);

        Assert.Equal(2, notifier.Sent.Count);
        Assert.Equal(new[] { 2, 3 }, notifier.Sent.Select(s => s.RecipientUserId).OrderBy(id => id).ToArray());
        Assert.All(notifier.Sent, s => Assert.Equal("post", s.Notification.Type));
        Assert.DoesNotContain(notifier.Sent, s => s.RecipientUserId == 4); // non-follower not notified
    }

    [Fact]
    public async Task CreatePost_with_no_followers_sends_nothing()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db, userId: 1);
        var notifier = new SpyNotificationService();
        var handler = new CreatePostCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        await handler.Handle(new CreatePostCommand { BookId = 1, ReviewText = "x" }, CancellationToken.None);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task CreatePost_throws_when_book_missing()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new CreatePostCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new CreatePostCommand { BookId = 999, ReviewText = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task ToggleLike_adds_then_removes_and_updates_count()
    {
        using var db = CreateContext();
        var (author, book) = await SeedAuthorAndBookAsync(db, userId: 1);
        var post = new Post { Id = 1, UserId = author.Id, BookId = book.Id, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "r", CreatedAt = DateTime.UtcNow };
        db.Posts.Add(post);
        // a second user does the liking
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "User 2", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var notifier = new SpyNotificationService();
        var handler = new ToggleLikeCommandHandler(db, new FakeCurrentUserService(userId: 2), notifier);

        var liked = await handler.Handle(new ToggleLikeCommand(1), CancellationToken.None);
        Assert.True(liked.IsLiked);
        Assert.Equal(1, liked.LikeCount);
        Assert.Single(notifier.Sent); // author notified
        Assert.Equal(1, notifier.Sent[0].RecipientUserId);
        Assert.Equal("like", notifier.Sent[0].Notification.Type);

        var unliked = await handler.Handle(new ToggleLikeCommand(1), CancellationToken.None);
        Assert.False(unliked.IsLiked);
        Assert.Equal(0, unliked.LikeCount);
        Assert.Single(notifier.Sent); // un-like does not notify again
    }

    [Fact]
    public async Task ToggleLike_does_not_notify_when_liking_own_post()
    {
        using var db = CreateContext();
        var (author, book) = await SeedAuthorAndBookAsync(db, userId: 1);
        db.Posts.Add(new Post { Id = 1, UserId = author.Id, BookId = book.Id, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "r", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var notifier = new SpyNotificationService();
        var handler = new ToggleLikeCommandHandler(db, new FakeCurrentUserService(userId: 1), notifier);

        await handler.Handle(new ToggleLikeCommand(1), CancellationToken.None);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task ToggleLike_throws_when_post_missing()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new ToggleLikeCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ToggleLikeCommand(123), CancellationToken.None));
    }

    [Fact]
    public async Task GetFeed_blends_followed_first_then_non_followed()
    {
        using var db = CreateContext();
        var book = new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 };
        db.Books.Add(book);
        foreach (var id in new[] { 1, 2, 3 })
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        // user 1 follows user 2 (not user 3)
        db.Follows.Add(new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow });
        db.Posts.Add(new Post { Id = 1, UserId = 1, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "own", CreatedAt = DateTime.UtcNow });
        db.Posts.Add(new Post { Id = 2, UserId = 2, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "followed", CreatedAt = DateTime.UtcNow });
        db.Posts.Add(new Post { Id = 3, UserId = 3, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "stranger", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(db, new FakeCurrentUserService(userId: 1));
        var page = await handler.Handle(new GetFeedQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        // Non-followed authors are now part of the feed too (the 40% share).
        Assert.Equal(3, page.TotalCount);
        var authorIds = page.Items.Select(p => p.Post!.Author.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, authorIds);
        // Followed (+ own) posts lead; the stranger comes after them.
        Assert.Equal(3, page.Items[^1].Post!.Author.Id);
    }

    [Fact]
    public async Task GetFeed_falls_back_to_global_when_not_following_anyone()
    {
        using var db = CreateContext();
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        foreach (var id in new[] { 1, 2 })
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        db.Posts.Add(new Post { Id = 1, UserId = 2, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "global", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(db, new FakeCurrentUserService(userId: 1));
        var page = await handler.Handle(new GetFeedQuery(), CancellationToken.None);

        Assert.Equal(1, page.TotalCount); // sees a stranger's post because it follows no one
    }

    [Fact]
    public async Task GetFeed_paginates_and_clamps_page_size()
    {
        using var db = CreateContext();
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        db.Users.Add(new User { Id = 1, GoogleId = "g-1", Email = "u1@e.com", FullName = "U1", CreatedAt = DateTime.UtcNow });
        for (var i = 1; i <= 5; i++)
        {
            db.Posts.Add(new Post { Id = i, UserId = 1, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = $"p{i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        }
        await db.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(db, new FakeCurrentUserService(userId: 1));
        var page = await handler.Handle(new GetFeedQuery { Page = 1, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
        // newest first
        Assert.Equal("p5", page.Items[0].Post!.ReviewText);
    }

    [Fact]
    public async Task GetFeed_mixes_60_40_and_pages_without_duplicates()
    {
        using var db = CreateContext();
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = 100 });
        foreach (var id in new[] { 1, 2, 3 })
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        db.Follows.Add(new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow });
        // 10 followed posts (user 2) and 10 non-followed posts (user 3), distinct timestamps.
        var pid = 1;
        for (var i = 0; i < 10; i++)
        {
            db.Posts.Add(new Post { Id = pid++, UserId = 2, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = $"f{i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
            db.Posts.Add(new Post { Id = pid++, UserId = 3, BookId = 1, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = $"n{i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        }
        await db.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(db, new FakeCurrentUserService(userId: 1));
        var page1 = await handler.Handle(new GetFeedQuery { Page = 1, PageSize = 5 }, CancellationToken.None);

        Assert.Equal(20, page1.TotalCount);
        // 3 followed : 2 non-followed on a 5-item page.
        Assert.Equal(3, page1.Items.Count(p => p.Post!.Author.Id == 2));
        Assert.Equal(2, page1.Items.Count(p => p.Post!.Author.Id == 3));

        var page2 = await handler.Handle(new GetFeedQuery { Page = 2, PageSize = 5 }, CancellationToken.None);
        // No item appears on both pages.
        var ids1 = page1.Items.Select(p => p.Post!.Id).ToHashSet();
        var ids2 = page2.Items.Select(p => p.Post!.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
        // Scrolling deeper shows older items within each bucket (followed and non-followed).
        foreach (var authorId in new[] { 2, 3 })
        {
            var newestOnPage2 = page2.Items.Where(p => p.Post!.Author.Id == authorId).Max(p => p.CreatedAt);
            var oldestOnPage1 = page1.Items.Where(p => p.Post!.Author.Id == authorId).Min(p => p.CreatedAt);
            Assert.True(oldestOnPage1 >= newestOnPage2);
        }
    }

    [Fact]
    public async Task DeleteComment_removes_comment_and_its_replies_for_author()
    {
        using var db = CreateContext();
        var (author, book) = await SeedAuthorAndBookAsync(db, userId: 1);
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "User 2", CreatedAt = DateTime.UtcNow });
        db.Posts.Add(new Post { Id = 1, UserId = author.Id, BookId = book.Id, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "r", CreatedAt = DateTime.UtcNow });
        db.Comments.Add(new Comment { Id = 1, PostId = 1, UserId = 1, Text = "ota izoh", CreatedAt = DateTime.UtcNow });
        db.Comments.Add(new Comment { Id = 2, PostId = 1, UserId = 2, Text = "javob", ParentCommentId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new DeleteCommentCommandHandler(db, new FakeCurrentUserService(userId: 1));
        await handler.Handle(new DeleteCommentCommand(1), CancellationToken.None);

        Assert.Empty(await db.Comments.ToListAsync()); // parent + reply both gone
    }

    [Fact]
    public async Task DeleteComment_throws_forbidden_for_non_author()
    {
        using var db = CreateContext();
        var (author, book) = await SeedAuthorAndBookAsync(db, userId: 1);
        db.Users.Add(new User { Id = 2, GoogleId = "g-2", Email = "u2@e.com", FullName = "User 2", CreatedAt = DateTime.UtcNow });
        db.Posts.Add(new Post { Id = 1, UserId = author.Id, BookId = book.Id, Slug = "s" + System.Guid.NewGuid().ToString("N").Substring(0,8), ReviewText = "r", CreatedAt = DateTime.UtcNow });
        db.Comments.Add(new Comment { Id = 1, PostId = 1, UserId = 1, Text = "izoh", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new DeleteCommentCommandHandler(db, new FakeCurrentUserService(userId: 2));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new DeleteCommentCommand(1), CancellationToken.None));
        Assert.Single(await db.Comments.ToListAsync()); // still there
    }

    [Fact]
    public async Task DeleteComment_throws_when_comment_missing()
    {
        using var db = CreateContext();
        await SeedAuthorAndBookAsync(db);
        var handler = new DeleteCommentCommandHandler(db, new FakeCurrentUserService(userId: 1));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteCommentCommand(999), CancellationToken.None));
    }
}
