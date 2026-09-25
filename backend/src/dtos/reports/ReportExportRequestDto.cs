using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Dtos.Reports;

public class ReportExportRequestDto
{
    /// <summary>One of: PriceTrends, Listings, Orders.</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateRangeStart { get; set; }

    [Required]
    public DateOnly? DateRangeEnd { get; set; }
}
