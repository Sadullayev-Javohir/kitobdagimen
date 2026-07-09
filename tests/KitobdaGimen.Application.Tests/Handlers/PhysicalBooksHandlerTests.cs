using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.AddPhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.CancelReservation;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ConfirmHandover;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.RemovePhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReservePhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReturnPhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetLibrary;
using KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetMyPhysicalBooks;
using KitobdaGimen.Application.Tests.Support;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Handlers;

/// <summary>
/// Integratsion testlar: to'liq kitob almashish oqimini tekshiradi (qo'shish, band qilish,
/// topshirish, qaytarish, bekor qilish, o'chirish) — barcha statuslar o'zgarishi va
/// bildirishnomalar yuborilishi.
/// </summary>
public class PhysicalBooksHandlerTests : TestBase
{
    [Fact]
    public async Task FullFlow_AddReserveConfirmReturn_Success()
    {
        // Arrange
        var db = CreateContext();
        var owner = new User { Id = 1, FullName = "Alice", Email = "alice@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        var reserver = new User { Id = 2, FullName = "Bob", Email = "bob@test.uz", GoogleId = "g-2", CreatedAt = DateTime.UtcNow };
        var catalogBook = new Book { Id = 10, Title = "Test Kitob", Author = "Test Muallif", TotalPages = 100 };
        db.Users.AddRange(owner, reserver);
        db.Books.Add(catalogBook);
        await db.SaveChangesAsync();

        var ownerSvc = new FakeCurrentUserService(owner.Id);
        var reserverSvc = new FakeCurrentUserService(reserver.Id);
        var notifications = new SpyNotificationService();

        // Act 1: Egasi kitob qo'shadi (katalogdan)
        var addHandler = new AddPhysicalBookCommandHandler(db, ownerSvc);
        var added = await addHandler.Handle(new AddPhysicalBookCommand { BookId = catalogBook.Id }, default);

        Assert.Equal(catalogBook.Title, added.Title);
        Assert.Equal(PhysicalBookStatus.Mavjud, added.Status);
        Assert.True(added.IsMine);

        // Act 2: Boshqa foydalanuvchi band qiladi
        var reserveHandler = new ReservePhysicalBookCommandHandler(db, reserverSvc, notifications);
        var reserved = await reserveHandler.Handle(new ReservePhysicalBookCommand(added.Id), default);

        Assert.Equal(PhysicalBookStatus.BandQilindi, reserved.Status);
        Assert.True(reserved.ReservedByMe);
        Assert.NotNull(reserved.ReservationExpiresAt);
        // Egaga bildirishnoma borligini tekshiramiz.
        Assert.Single(notifications.Sent);
        Assert.Equal(owner.Id, notifications.Sent[0].RecipientUserId);
        Assert.Contains("o'qimoqchi", notifications.Sent[0].Notification.Message);

        // Act 3: Egasi topshirishni tasdiqlaydi
        notifications.Sent.Clear();
        var confirmHandler = new ConfirmHandoverCommandHandler(db, ownerSvc, notifications);
        var confirmed = await confirmHandler.Handle(new ConfirmHandoverCommand(added.Id), default);

        Assert.Equal(PhysicalBookStatus.OqiyApti, confirmed.Status);
        Assert.Single(notifications.Sent);
        Assert.Equal(reserver.Id, notifications.Sent[0].RecipientUserId);
        Assert.Contains("topshirildi", notifications.Sent[0].Notification.Message);

        // Act 4: Egasi qaytarib olganini belgilaydi
        var returnHandler = new ReturnPhysicalBookCommandHandler(db, ownerSvc);
        var returned = await returnHandler.Handle(new ReturnPhysicalBookCommand(added.Id), default);

        Assert.Equal(PhysicalBookStatus.Mavjud, returned.Status);
    }

    [Fact]
    public async Task ReserveOwnBook_ThrowsForbidden()
    {
        var db = CreateContext();
        var owner = new User { Id = 1, FullName = "Owner", Email = "owner@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        db.Users.Add(owner);
        db.PhysicalBooks.Add(new PhysicalBook
        {
            Id = 1,
            OwnerId = owner.Id,
            ManualTitle = "Kitob",
            Status = PhysicalBookStatus.Mavjud,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new ReservePhysicalBookCommandHandler(db, new FakeCurrentUserService(owner.Id), new SpyNotificationService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ReservePhysicalBookCommand(1), default));
    }

    [Fact]
    public async Task CancelReservation_ByReserver_Success()
    {
        var db = CreateContext();
        var owner = new User { Id = 1, FullName = "Owner", Email = "owner@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        var reserver = new User { Id = 2, FullName = "Reserver", Email = "reserver@test.uz", GoogleId = "g-2", CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(owner, reserver);
        var book = new PhysicalBook
        {
            Id = 1, OwnerId = owner.Id, ManualTitle = "Kitob", Status = PhysicalBookStatus.BandQilindi, CreatedAt = DateTime.UtcNow
        };
        var reservation = new PhysicalBookReservation
        {
            Id = 1, PhysicalBookId = book.Id, ReserverId = reserver.Id,
            ReservedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(24), IsConfirmed = false
        };
        db.PhysicalBooks.Add(book);
        db.PhysicalBookReservations.Add(reservation);
        await db.SaveChangesAsync();

        var handler = new CancelReservationCommandHandler(db, new FakeCurrentUserService(reserver.Id), new SpyNotificationService());
        var result = await handler.Handle(new CancelReservationCommand(book.Id), default);

        Assert.Equal(PhysicalBookStatus.Mavjud, result.Status);
        // Band qilish o'chirilgan
        Assert.Empty(await db.PhysicalBookReservations.ToListAsync());
    }

    [Fact]
    public async Task RemoveBook_OnlyWhenAvailable()
    {
        var db = CreateContext();
        var owner = new User { Id = 1, FullName = "Owner", Email = "owner@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        db.Users.Add(owner);
        db.PhysicalBooks.Add(new PhysicalBook
        {
            Id = 1, OwnerId = owner.Id, ManualTitle = "Kitob", Status = PhysicalBookStatus.OqiyApti, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RemovePhysicalBookCommandHandler(db, new FakeCurrentUserService(owner.Id));

        // O'qilayotgan kitobni o'chirib bo'lmaydi
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new RemovePhysicalBookCommand(1), default));

        // Mavjud qilsak — o'chadi
        var book = await db.PhysicalBooks.FindAsync(1);
        book!.Status = PhysicalBookStatus.Mavjud;
        await db.SaveChangesAsync();
        await handler.Handle(new RemovePhysicalBookCommand(1), default);

        Assert.Empty(await db.PhysicalBooks.ToListAsync());
    }

    [Fact]
    public async Task GetLibrary_ExcludesOwnButShowsAllStatuses()
    {
        var db = CreateContext();
        var user1 = new User { Id = 1, FullName = "User1", Email = "u1@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, FullName = "User2", Email = "u2@test.uz", GoogleId = "g-2", CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(user1, user2);
        db.PhysicalBooks.AddRange(
            new PhysicalBook { Id = 1, OwnerId = user1.Id, ManualTitle = "Mine", Status = PhysicalBookStatus.Mavjud, CreatedAt = DateTime.UtcNow },
            new PhysicalBook { Id = 2, OwnerId = user2.Id, ManualTitle = "Available", Status = PhysicalBookStatus.Mavjud, CreatedAt = DateTime.UtcNow },
            new PhysicalBook { Id = 3, OwnerId = user2.Id, ManualTitle = "Reserved", Status = PhysicalBookStatus.BandQilindi, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var handler = new GetLibraryQueryHandler(db, new FakeCurrentUserService(user1.Id));
        var lib = await handler.Handle(new GetLibraryQuery(), default);

        // O'zining kitobi (id=1) ko'rinmaydi; user2'ning barcha statusdagi kitoblari ko'rinadi.
        // Status bo'yicha tartiblangan: avval Mavjud (id=2), keyin BandQilindi (id=3).
        Assert.Equal(2, lib.Count);
        Assert.Equal(2, lib[0].Id);
        Assert.Equal(3, lib[1].Id);
    }

    [Fact]
    public async Task GetMyPhysicalBooks_ShowsAllStatuses()
    {
        var db = CreateContext();
        var owner = new User { Id = 1, FullName = "Owner", Email = "owner@test.uz", GoogleId = "g-1", CreatedAt = DateTime.UtcNow };
        db.Users.Add(owner);
        db.PhysicalBooks.AddRange(
            new PhysicalBook { Id = 1, OwnerId = owner.Id, ManualTitle = "Book1", Status = PhysicalBookStatus.Mavjud, CreatedAt = DateTime.UtcNow },
            new PhysicalBook { Id = 2, OwnerId = owner.Id, ManualTitle = "Book2", Status = PhysicalBookStatus.BandQilindi, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var handler = new GetMyPhysicalBooksQueryHandler(db, new FakeCurrentUserService(owner.Id));
        var mine = await handler.Handle(new GetMyPhysicalBooksQuery(), default);

        Assert.Equal(2, mine.Count);
        Assert.All(mine, b => Assert.True(b.IsMine));
    }
}
