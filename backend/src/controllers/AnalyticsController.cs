using System.ComponentModel.DataAnnotations;
using AgriConnect.Api.Dtos.Analytics;
using AgriConnect.Api.Dtos.Common;
using AgriConnect.Api.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>Component D — Market Price Analytics (FR15–FR17).</summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Officer,Administrator")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class AnalyticsController(
    TrendAggregationService trends,
    AnomalyDetectionService anomalies,
    ShortageDetectionService shortages,
    AnomalyInvestigationService investigation) : ControllerBase
{
    /// <summary>Historical price trend for a crop, per region or all regions combined (FR15).</summary>
    [HttpGet("price-trends")]
    [ProducesResponseType(typeof(PriceTrendResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceTrendResponseDto>> GetPriceTrends(
        [FromQuery, Required] Guid? cropId,
        [FromQuery] Guid? regionId,
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        [FromQuery] string bucket = "week")
    {
        if (to < from)
        {
            ModelState.AddModelError(nameof(to), "'to' must be on or after 'from'.");
        }
        if (!TrendAggregationService.Buckets.Contains(bucket, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(bucket),
                $"'bucket' must be one of: {string.Join(", ", TrendAggregationService.Buckets)}.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        var rows = await trends.GetSnapshotsAsync(cropId!.Value, regionId, from!.Value, to!.Value);

        return new PriceTrendResponseDto
        {
            CropId = cropId.Value,
            RegionId = regionId,
            Bucket = bucket.ToLowerInvariant(),
            Points = TrendAggregationService.BuildPoints(rows, bucket),
        };
    }

    /// <summary>Paged review queue of listings priced away from the AI suggestion (FR16).</summary>
    /// <param name="status">Open, Reviewed or Dismissed. Omit for all statuses.</param>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(PagedResponseDto<AnomalyFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponseDto<AnomalyFlagDto>>> GetAnomalies(
        [FromQuery] string? status,
        [FromQuery] Guid? cropId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, AnomalyDetectionService.MaxPageSize)] int size = 20)
    {
        var flags = await anomalies.GetFlagsAsync(status, cropId, page, size);

        return new PagedResponseDto<AnomalyFlagDto>
        {
            Items = flags.Select(AnomalyFlagDto.From).ToList(),
            Page = page,
            Size = size,
            Total = await anomalies.CountFlagsAsync(status, cropId),
        };
    }

    /// <summary>Explains why a listing was flagged and ranks the likely causes (FR16).</summary>
    [HttpGet("anomalies/{listingId:guid}/investigate")]
    [ProducesResponseType(typeof(InvestigationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestigationResultDto>> Investigate(Guid listingId) =>
        await investigation.InvestigateAsync(listingId);

    /// <summary>Officer triage: mark a flag Reviewed or Dismissed.</summary>
    [HttpPatch("anomalies/{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AnomalyFlagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnomalyFlagDto>> UpdateAnomalyStatus(Guid id, [FromBody] UpdateAnomalyStatusDto request)
    {
        await anomalies.UpdateStatusAsync(id, request.Status);
        var flag = await anomalies.GetFlagByIdAsync(id);
        return AnomalyFlagDto.From(flag!);
    }

    /// <summary>Recurring shortage and oversupply events (FR17).</summary>
    /// <param name="type">Shortage or Oversupply.</param>
    /// <param name="severity">Low, Medium or High.</param>
    [HttpGet("shortages")]
    [ProducesResponseType(typeof(ItemsResponseDto<ShortageEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemsResponseDto<ShortageEventDto>>> GetShortages(
        [FromQuery] Guid? cropId,
        [FromQuery] Guid? regionId,
        [FromQuery] string? type,
        [FromQuery] string? severity)
    {
        var events = await shortages.GetEventsAsync(cropId, regionId, type, severity);
        return new ItemsResponseDto<ShortageEventDto> { Items = events.Select(ShortageEventDto.From).ToList() };
    }

    /// <summary>Rebuilds the weekly trend table for every crop and region. Idempotent.</summary>
    [HttpPost("snapshots/refresh")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(SnapshotRefreshResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SnapshotRefreshResultDto>> RefreshSnapshots() =>
        await trends.RefreshAllAsync();
}
