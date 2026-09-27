using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Dtos.Analytics;

public class UpdateAnomalyStatusDto
{
    /// <summary>Reviewed or Dismissed.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
