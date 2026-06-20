using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.Profile.Commands.DeleteAccount;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = KitobdaGimen.Application.Common.Exceptions.ValidationException;

namespace KitobdaGimen.Application.Tests.Handlers;

public class DeleteAccountHandlerTests : TestBase
{
    private static async Task SeedUsersAsync(TestDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Users.Add(new User { Id = id, GoogleId = $"g-{id}", Email = $"u{id}@e.com", FullName = $"U{id}", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Deletes_user_and_removes_connections_in_both_directions()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1, 2, 3);
        db.Connections.Add(new Connection { RequesterId = 1, AddresseeId = 2, Status = ConnectionStatus.Accepted, CreatedAt = DateTime.UtcNow });
        db.Connections.Add(new Connection { RequesterId = 3, AddresseeId = 1, Status = ConnectionStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await handler.Handle(new DeleteAccountCommand("u1@e.com"), CancellationToken.None);

        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
        Assert.Empty(await db.Connections.ToListAsync()); // both connections involving user 1 gone
        Assert.Equal(2, await db.Users.CountAsync()); // users 2 and 3 remain
    }

    [Fact]
    public async Task Wrong_email_does_not_delete()
    {
        using var db = CreateContext();
        await SeedUsersAsync(db, 1);
        var handler = new DeleteAccountCommandHandler(db, new FakeCurrentUserService(userId: 1, email: "u1@e.com"));

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DeleteAccountCommand("wrong@e.com"), CancellationToken.None));

        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Id == 1));
    }
}
