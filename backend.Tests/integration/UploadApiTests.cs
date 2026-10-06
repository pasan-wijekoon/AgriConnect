using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AgriConnect.Api.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Produce photos live in the database: POST /api/upload stores the bytes, GET /uploads/{name}
/// serves them, nothing is written to the API server's disk.
/// </summary>
public class UploadApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private static readonly byte[] Png = Convert.FromHexString(
        "89504E470D0A1A0A0000000D49484452000000010000000108060000001F15C4890000000D49444154789C6360000002000001E221BC330000000049454E44AE426082");

    private readonly ApiTestFactory _factory;
    private readonly List<string> _uploaded = [];

    public UploadApiTests(ApiTestFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>Shared dev database: remove the photos these tests created.</summary>
    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriConnect.Api.Config.AgriConnectDbContext>();
        await db.UploadedImages.Where(i => _uploaded.Contains(i.FileName)).ExecuteDeleteAsync();
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { part, "file", fileName } };
    }

    private async Task<(string Url, string FileName)> UploadPngAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/upload", FileContent(Png, "photo.png", "image/png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        var fileName = json.GetProperty("fileName").GetString()!;
        _uploaded.Add(fileName);
        return (json.GetProperty("url").GetString()!, fileName);
    }

    [Fact]
    public async Task Upload_StoresTheImageInTheDatabase_AndItCanBeFetchedAnonymously()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        var (url, _) = await UploadPngAsync(farmer);

        Assert.StartsWith("/uploads/", url);
        Assert.EndsWith(".png", url);

        using var anonymous = _factory.CreateAuthedClient(); // <img> tags carry no token
        var image = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync());
        Assert.Contains("immutable", image.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Upload_DoesNotWriteAFileToTheApiServersDisk()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);
        var (_, fileName) = await UploadPngAsync(farmer);

        var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
        var onDisk = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", fileName);

        Assert.False(File.Exists(onDisk));
    }

    [Fact]
    public async Task Upload_DetectsTheRealFormat_NotTheFileNameOrDeclaredType()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        // A PNG named .jpg and declared as jpeg is stored and served as the PNG it is.
        var response = await farmer.PostAsync("/api/upload", FileContent(Png, "photo.jpg", "image/jpeg"));
        var json = (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        _uploaded.Add(json.GetProperty("fileName").GetString()!);

        Assert.EndsWith(".png", json.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("<html><script>alert(1)</script></html>", "evil.jpg", "image/jpeg")]
    [InlineData("just some text", "notes.png", "image/png")]
    [InlineData("MZ-not-an-image", "run.exe", "application/octet-stream")]
    public async Task Upload_OfSomethingThatIsNotAnImage_Returns400(string content, string name, string type)
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await farmer.PostAsync("/api/upload", FileContent(System.Text.Encoding.UTF8.GetBytes(content), name, type));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutAFile_Returns400()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await farmer.PostAsync("/api/upload", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutCredentials_Returns401()
    {
        using var anonymous = _factory.CreateAuthedClient();

        var response = await anonymous.PostAsync("/api/upload", FileContent(Png, "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/uploads/00000000-0000-0000-0000-000000000000.png")] // well-formed, not stored
    [InlineData("/uploads/not-a-guid.png")]
    [InlineData("/uploads/..%2Fappsettings.json")]
    [InlineData("/uploads/6f1c1e2a-0000-4000-8000-000000000000.exe")]
    public async Task Serving_AnUnknownOrMalformedName_Returns404(string url)
    {
        using var anonymous = _factory.CreateAuthedClient();

        var response = await anonymous.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
