namespace AgriConnect.Api.Dtos.Analytics;

public class AnalyticsFiltersDto
{
    public List<NamedItemDto> Crops { get; set; } = [];

    public List<NamedItemDto> Regions { get; set; } = [];
}

public class NamedItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
