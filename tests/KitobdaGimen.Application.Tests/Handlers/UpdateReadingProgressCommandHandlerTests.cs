using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

public class UpdateReadingProgressCommandHandlerTests : TestBase
{
    private static async Task<ReadingGoal> SeedGoalAsync(TestDbContext db, int userId = 1, int totalPages = 300, int currentPage = 0)
    {
        db.Users.Add(new User { Id = userId, GoogleId = $"g-{userId}", Email = $"u{userId}@e.com", FullName = $"U{userId}", CreatedAt = DateTime.UtcNow });
        db.Books.Add(new Book { Id = 1, Title = "B", Author = "A", TotalPages = totalPages });
        var goal = new ReadingGoal { Id = 1, UserId = userId, BookId = 1, DailyPageGoal = 20, StartDate = DateTime.UtcNow, CurrentPage = currentPage, IsActive = true };
        db.ReadingGoals.Add(goal);
        await db.SaveChangesAsync();
        return goal;
    }

    [Fact]
    public async Task First_update_creates_today_progress_and_advances_page()
    {
        using var db = CreateContext();
        await SeedGoalAsync(db);
        var handler = new UpdateReadingProgressCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        var dto = await handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 30 }, CancellationToken.None);

        Assert.Equal(30, dto.CurrentPage);
        Assert.Equal(30, dto.PagesReadToday);
        Assert.True(dto.IsActive);
        Assert.Single(await db.ReadingProgress.ToListAsync());
    }

    [Fact]
    public async Task Second_update_same_day_accumulates_pages()
    {
        using var db = CreateContext();
        await SeedGoalAsync(db);
        var handler = new UpdateReadingProgressCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 20 }, CancellationToken.None);
        var dto = await handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 15 }, CancellationToken.None);

        Assert.Equal(35, dto.CurrentPage);
        Assert.Equal(35, dto.PagesReadToday);
        Assert.Single(await db.ReadingProgress.ToListAsync()); // upsert, not a new row
    }

    [Fact]
    public async Task Reaching_total_pages_completes_goal()
    {
        using var db = CreateContext();
        await SeedGoalAsync(db, totalPages: 100, currentPage: 90);
        var handler = new UpdateReadingProgressCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        var dto = await handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 50 }, CancellationToken.None);

        Assert.Equal(100, dto.CurrentPage); // capped at total
        Assert.False(dto.IsActive);          // finished
        Assert.Equal(100, dto.ProgressPercent);
    }

    [Fact]
    public async Task Cannot_update_another_users_goal()
    {
        using var db = CreateContext();
        await SeedGoalAsync(db, userId: 1);
        var handler = new UpdateReadingProgressCommandHandler(db, new FakeCurrentUserService(userId: 2), new SpyNotificationService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 10 }, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_goal_missing()
    {
        using var db = CreateContext();
        await SeedGoalAsync(db, userId: 1);
        var handler = new UpdateReadingProgressCommandHandler(db, new FakeCurrentUserService(userId: 1), new SpyNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new UpdateReadingProgressCommand { ReadingGoalId = 999, PagesRead = 10 }, CancellationToken.None));
    }
}
