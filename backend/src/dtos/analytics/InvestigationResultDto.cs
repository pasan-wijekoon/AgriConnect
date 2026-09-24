namespace AgriConnect.Api.Dtos.Analytics;

public class InvestigationResultDto
{
    public Guid ListingId { get; set; }

    public FlagSummaryDto Flag { get; set; } = new();

    public PriceContextDto PriceContext { get; set; } = new();

    public ExternalContextDto InspectionContext { get; set; } = new();

    public ExternalContextDto OrderContext { get; set; } = new();

    /// <summary>Most likely cause first. Never empty.</summary>
    public List<LikelyCauseDto> LikelyCauses { get; set; } = [];
}

public class FlagSummaryDto
{
    public decimal DeviationPercent { get; set; }

    public DateTimeOffset FlaggedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class PriceContextDto
{
    /// <summary>Null when the crop has no price history in this region yet.</summary>
    public decimal? RegionalAvgPrice { get; set; }

    public decimal ListingPrice { get; set; }

    /// <summary>0–100. Share of this crop's weekly averages in the region priced below the listing.</summary>
    public int PercentileInRegion { get; set; }
}

/// <summary>Data owned by another component. Keys stay stable while it is unavailable.</summary>
public class ExternalContextDto
{
    public bool Available { get; set; }

    public string? Reason { get; set; }
}

public class LikelyCauseDto
{
    public string Cause { get; set; } = string.Empty;

    public string Confidence { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;
}
