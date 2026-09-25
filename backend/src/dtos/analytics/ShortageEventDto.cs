using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos.Analytics;

public class ShortageEventDto
{
    public Guid Id { get; set; }

    public Guid CropId { get; set; }

    public Guid RegionId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; }

    public string? Notes { get; set; }

    public static ShortageEventDto From(ShortageOversupplyEvent e) => new()
    {
        Id = e.Id,
        CropId = e.CropId,
        RegionId = e.RegionId,
        Type = e.Type.ToString(),
        Severity = e.Severity.ToString(),
        DetectedAt = e.DetectedAt,
        Notes = e.Notes,
    };
}
