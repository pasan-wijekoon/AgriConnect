using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace backend.Tests.services;

public class UploadedImageImporterTests : IDisposable
{
    private static readonly byte[] Png = Convert.FromHexString("89504E470D0A1A0A0000000D4948445200000001");
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "agri-uploads-" + Guid.NewGuid().ToString("N"));

    public UploadedImageImporterTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static AgriConnectDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task ImportAsync_CopiesExistingPhotosIntoTheDatabase_UnderTheSameName()
    {
        var png = $"{Guid.NewGuid()}.png";
        var jpeg = $"{Guid.NewGuid()}.jpeg"; // older uploads kept the original extension
        await File.WriteAllBytesAsync(Path.Combine(_dir, png), Png);
        await File.WriteAllBytesAsync(Path.Combine(_dir, jpeg), Jpeg);
        await using var db = NewDb();

        var imported = await UploadedImageImporter.ImportAsync(db, _dir, NullLogger.Instance);

        Assert.Equal(2, imported);
        var stored = await db.UploadedImages.ToDictionaryAsync(i => i.FileName);
        Assert.Equal("image/png", stored[png].ContentType);
        Assert.Equal(Png, stored[png].Data);
        Assert.Equal("image/jpeg", stored[jpeg].ContentType);
    }

    [Fact]
    public async Task ImportAsync_IsIdempotent()
    {
        await File.WriteAllBytesAsync(Path.Combine(_dir, $"{Guid.NewGuid()}.png"), Png);
        await using var db = NewDb();

        await UploadedImageImporter.ImportAsync(db, _dir, NullLogger.Instance);
        var second = await UploadedImageImporter.ImportAsync(db, _dir, NullLogger.Instance);

        Assert.Equal(0, second);
        Assert.Equal(1, await db.UploadedImages.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_SkipsFilesThatAreNotImagesOrNotNamedByUs()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, $"{Guid.NewGuid()}.png"), "not an image");
        await File.WriteAllBytesAsync(Path.Combine(_dir, "holiday.png"), Png);
        await using var db = NewDb();

        var imported = await UploadedImageImporter.ImportAsync(db, _dir, NullLogger.Instance);

        Assert.Equal(0, imported);
    }

    [Fact]
    public async Task ImportAsync_WithNoUploadsFolder_DoesNothing()
    {
        await using var db = NewDb();

        var imported = await UploadedImageImporter.ImportAsync(db, Path.Combine(_dir, "missing"), NullLogger.Instance);

        Assert.Equal(0, imported);
    }
}
