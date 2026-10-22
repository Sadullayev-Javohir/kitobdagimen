using KitobdaGimen.Application.Features.Books.Queries.GetBooksFeed;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KitobdaGimen.Application.Tests.Handlers;

public class GetBooksFeedQueryHandlerTests
{
    [Fact]
    public async Task Search_Returns_Books_Without_Posts_Or_Quotes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        db.Books.Add(new Book
        {
            Id = 15,
            Title = "Baxtiyor oila (kirill)",
            Author = "Shayx Muhammad Sodiq Muhammad Yusuf",
            TotalPages = 400
        });
        await db.SaveChangesAsync();

        var handler = new GetBooksFeedQueryHandler(db);
        var result = await handler.Handle(new GetBooksFeedQuery { Search = "baxtiyor" }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Baxtiyor oila (kirill)", result.Items[0].Title);
    }
}
