using KitobdaGimen.Application.Features.Users.Dtos;
using KitobdaGimen.Application.Features.Users.Queries.SearchUsers;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;

namespace KitobdaGimen.Application.Tests.Handlers;

public class SearchUsersHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params (int Id, string Name, string? Username)[] users)
    {
        foreach (var u in users)
        {
            db.Users.Add(new User { Id = u.Id, GoogleId = $"g-{u.Id}", Email = $"u{u.Id}@e.com", FullName = u.Name, Username = u.Username, CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Finds_by_name_excludes_self_and_reports_connection_state()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db,
            (1, "Ali Valiyev", "ali"),
            (2, "Vali Aliyev", "vali"),
            (3, "Hasan Hasanov", "hasan"));
        // 1 → 2 pending (outgoing from 1's view); 1 ↔ 3 accepted.
        db.Connections.Add(new Connection { RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        db.Connections.Add(new Connection { RequesterId = 3, AddresseeId = 1, Status = ConnectionStatus.Accepted, CreatedAt = DateTime.UtcNow, RespondedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new SearchUsersQueryHandler(db, new FakeCurrentUserService(userId: 1));

        var result = await handler.Handle(new SearchUsersQuery { Q = "li" }, CancellationToken.None);

        // "li" matches Ali (self, excluded) and Vali; not Hasan.
        Assert.DoesNotContain(result.Items, u => u.Id == 1);
        var vali = Assert.Single(result.Items, u => u.Id == 2);
        Assert.Equal(ConnectionState.PendingOutgoing, vali.ConnectionState);
    }

    [Fact]
    public async Task Empty_query_lists_others_with_connected_state()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, (1, "Me", "me"), (3, "Hasan", "hasan"));
        db.Connections.Add(new Connection { RequesterId = 3, AddresseeId = 1, Status = ConnectionStatus.Accepted, CreatedAt = DateTime.UtcNow, RespondedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new SearchUsersQueryHandler(db, new FakeCurrentUserService(userId: 1));

        var result = await handler.Handle(new SearchUsersQuery { Q = "" }, CancellationToken.None);

        var hasan = Assert.Single(result.Items, u => u.Id == 3);
        Assert.Equal(ConnectionState.Connected, hasan.ConnectionState);
    }
}
