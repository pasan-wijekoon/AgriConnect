using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos.Analytics;

public class AnomalyFlagDto
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public Guid CropId { get; set; }

    public Guid RegionId { get; set; }

    public decimal ListingPrice { get; set; }

    /// <summary>Signed: positive is above the AI-suggested price, negative below.</summary>
    public decimal DeviationPercent { get; set; }

    public DateTimeOffset FlaggedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public static AnomalyFlagDto From(PriceAnomalyFlag flag) => new()
    {
        Id = flag.Id,
        ListingId = flag.ListingId,
        CropId = flag.CropId,
        RegionId = flag.RegionId,
        ListingPrice = flag.ListingPrice,
        DeviationPercent = flag.DeviationPercent,
        FlaggedAt = flag.FlaggedAt,
        Status = flag.Status.ToString(),
    };
}
