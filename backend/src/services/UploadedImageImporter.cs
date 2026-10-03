using AgriConnect.Api.Config;
using AgriConnect.Api.Controllers;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// One-off, idempotent move of photos that were uploaded before images lived in the database:
/// every file in <c>wwwroot/uploads</c> that has no <see cref="UploadedImage"/> row yet is copied
/// in under the same file name, so existing <c>/uploads/{name}</c> URLs on listings keep working
/// even after the folder is deleted.
/// </summary>
public static class UploadedImageImporter
{
    public static async Task<int> ImportAsync(AgriConnectDbContext db, string uploadsDirectory, ILogger logger, CancellationToken ct = default)
    {
        if (!Directory.Exists(uploadsDirectory)) return 0;

        var existing = (await db.UploadedImages.AsNoTracking().Select(i => i.FileName).ToListAsync(ct)).ToHashSet();
        var imported = 0;

        foreach (var path in Directory.EnumerateFiles(uploadsDirectory))
        {
            var name = Path.GetFileName(path);
            if (!ImageKind.IsStoredName(name) || existing.Contains(name)) continue;

            var data = await File.ReadAllBytesAsync(path, ct);
            if (ImageKind.Detect(data) is null)
            {
                logger.LogWarning("Skipped {File}: not a recognised image.", name);
                continue;
            }

            db.UploadedImages.Add(new UploadedImage
            {
                Id = Guid.Parse(name.AsSpan(0, 36)),
                FileName = name,
                ContentType = ImageKind.ContentTypeForExtension(Path.GetExtension(name)),
                Data = data,
                Size = data.Length,
                CreatedAt = File.GetCreationTimeUtc(path),
            });
            imported++;
        }

        if (imported > 0) await db.SaveChangesAsync(ct);
        return imported;
    }
}
