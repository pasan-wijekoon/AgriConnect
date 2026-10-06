namespace AgriConnect.Api.Dtos.Analytics;

public class AnalyticsFiltersDto
{
    public List<CropFilterDto> Crops { get; set; } = [];

    public List<NamedItemDto> Regions { get; set; } = [];
}

public class NamedItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CropFilterDto : NamedItemDto
{
    /// <summary>False when no price trend exists yet, so clients can open on a crop that has data.</summary>
    public bool HasPriceHistory { get; set; }
}
