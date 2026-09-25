using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

public class NotificationServiceTests
{
    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task ListForUserAsync_ReturnsOnlyThatUsersNotifications_NewestFirst()
    {
        await using var db = NewInMemoryDb();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var service = new NotificationService(db);

        service.Notify(userId, "OrderPlaced", "first");
        await db.SaveChangesAsync();
        service.Notify(userId, "OrderStatusChanged", "second");
        service.Notify(otherUserId, "OrderPlaced", "not this user's");
        await db.SaveChangesAsync();

        var result = await service.ListForUserAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal("second", result[0].Message); // newest first
        Assert.Equal("first", result[1].Message);
    }

    [Fact]
    public async Task MarkReadAsync_OwnNotification_SetsReadAtAndReturnsTrue()
    {
        await using var db = NewInMemoryDb();
        var userId = Guid.NewGuid();
        var service = new NotificationService(db);
        service.Notify(userId, "OrderPlaced", "hello");
        await db.SaveChangesAsync();
        var id = (await service.ListForUserAsync(userId)).Single().Id;

        var ok = await service.MarkReadAsync(id, userId);

        Assert.True(ok);
        var reloaded = (await service.ListForUserAsync(userId)).Single();
        Assert.NotNull(reloaded.ReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_AnotherUsersNotification_ReturnsFalse_DoesNotMarkItRead()
    {
        await using var db = NewInMemoryDb();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var service = new NotificationService(db);
        service.Notify(ownerId, "OrderPlaced", "hello");
        await db.SaveChangesAsync();
        var id = (await service.ListForUserAsync(ownerId)).Single().Id;

        var ok = await service.MarkReadAsync(id, attackerId); // IDOR attempt

        Assert.False(ok);
        var reloaded = (await service.ListForUserAsync(ownerId)).Single();
        Assert.Null(reloaded.ReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_UnknownId_ReturnsFalse()
    {
        await using var db = NewInMemoryDb();
        var ok = await new NotificationService(db).MarkReadAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(ok);
    }
}
