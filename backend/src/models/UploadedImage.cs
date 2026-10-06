using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// A produce photo uploaded through <c>POST /api/upload</c>, stored in PostgreSQL (bytea) rather than
/// on the API server's disk. Listings and inspections keep only its URL (<c>/uploads/{FileName}</c>), so
/// the picture lives and is backed up with the rest of the data and survives redeploys.
/// </summary>
public class UploadedImage
{
    public Guid Id { get; set; }

    /// <summary>Public name, "{guid}.{ext}" - the last part of the URL stored on listings.</summary>
    [Required, MaxLength(60)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;

    public byte[] Data { get; set; } = [];

    public long Size { get; set; }

    /// <summary>The user who uploaded it (null for photos imported from the old on-disk uploads folder).</summary>
    public Guid? UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
