using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ListingPhoto
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    [Required]
    public string Url { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
