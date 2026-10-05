using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Produce photo upload. The image bytes are stored in the database (<see cref="UploadedImage"/>) and
/// served back from <c>GET /uploads/{fileName}</c>; clients and listings only ever hold that URL.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController(AgriConnectDbContext db) : ControllerBase
{
    private const long MaxBytes = 15 * 1024 * 1024;

    /// <summary>
    /// POST /api/upload — Upload produce photo from device
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadPhoto(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > MaxBytes)
        {
            return BadRequest(new { message = "File size exceeds the 15MB limit." });
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, ct);
        var data = stream.ToArray();

        // Trust the bytes, not the file name or the declared content type.
        var kind = ImageKind.Detect(data);
        if (kind is null)
        {
            return BadRequest(new { message = "Only JPG, PNG, WebP or GIF images can be uploaded." });
        }

        var id = Guid.NewGuid();
        var image = new UploadedImage
        {
            Id = id,
            FileName = $"{id}{kind.Extension}",
            ContentType = kind.ContentType,
            Data = data,
            Size = data.Length,
            UploadedByUserId = User.GetUserId(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.UploadedImages.Add(image);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            url = $"/uploads/{image.FileName}",
            fileName = image.FileName,
            size = image.Size
        });
    }

    /// <summary>
    /// GET /uploads/{fileName} — serve a stored photo. Anonymous on purpose: the photos appear in
    /// &lt;img&gt; tags, which cannot send a token (listings are already public to signed-in users, and the
    /// name is an unguessable GUID). Files still on disk under wwwroot/uploads are served by the static
    /// file middleware first.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/uploads/{fileName}")]
    public async Task<IActionResult> GetPhoto(string fileName, CancellationToken ct)
    {
        if (!ImageKind.IsStoredName(fileName))
        {
            return NotFound();
        }

        var image = await db.UploadedImages.AsNoTracking()
            .Where(i => i.FileName == fileName)
            .Select(i => new { i.Data, i.ContentType })
            .FirstOrDefaultAsync(ct);
        if (image is null)
        {
            return NotFound();
        }

        // The name never changes content, so browsers may cache it for a long time.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Data, image.ContentType);
    }
}

/// <summary>Recognises the image formats we accept from their leading bytes.</summary>
public sealed record ImageKind(string ContentType, string Extension)
{
    private static readonly ImageKind Jpeg = new("image/jpeg", ".jpg");
    private static readonly ImageKind Png = new("image/png", ".png");
    private static readonly ImageKind Gif = new("image/gif", ".gif");
    private static readonly ImageKind Webp = new("image/webp", ".webp");

    public static ImageKind? Detect(ReadOnlySpan<byte> d)
    {
        if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return Jpeg;
        if (d.Length >= 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47
            && d[4] == 0x0D && d[5] == 0x0A && d[6] == 0x1A && d[7] == 0x0A) return Png;
        if (d.Length >= 6 && d[0] == 'G' && d[1] == 'I' && d[2] == 'F' && d[3] == '8') return Gif;
        if (d.Length >= 12 && d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F'
            && d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P') return Webp;
        return null;
    }

    /// <summary>Only names we generate ("{guid}.jpg|jpeg|png|gif|webp") are looked up.</summary>
    public static bool IsStoredName(string name) =>
        name.Length is >= 40 and <= 41
        && Guid.TryParse(name.AsSpan(0, 36), out _)
        && name.AsSpan(36) is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";

    public static string ContentTypeForExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => Png.ContentType,
        ".gif" => Gif.ContentType,
        ".webp" => Webp.ContentType,
        _ => Jpeg.ContentType,
    };
}
