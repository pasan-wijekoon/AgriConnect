namespace AgriConnect.Api.Dtos.Analytics;

public class PriceTrendResponseDto
{
    public Guid CropId { get; set; }

    /// <summary>Null when the trend covers all regions combined.</summary>
    public Guid? RegionId { get; set; }

    public string Bucket { get; set; } = "week";

    /// <summary>Periods with no data are omitted, never returned as a zero price.</summary>
    public List<PriceTrendPointDto> Points { get; set; } = [];
}

public class PriceTrendPointDto
{
    /// <summary>Monday of the week, or the 1st of the month.</summary>
    public DateOnly Period { get; set; }

    public decimal AvgPrice { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public int SampleCount { get; set; }
}
