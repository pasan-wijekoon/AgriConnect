using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/prices")]
[Authorize]
public class PricesController : ControllerBase
{
    private readonly AgenticAiService _agenticAi;
    private readonly ListingService _listingService;
    private readonly TodayPriceCatalogService _catalogService;

    public PricesController(AgenticAiService agenticAi, ListingService listingService, TodayPriceCatalogService catalogService)
    {
        _agenticAi = agenticAi;
        _listingService = listingService;
        _catalogService = catalogService;
    }

    /// <summary>
    /// GET /api/prices/today — Live fair-price estimates for the admin-managed
    /// Today's Prices catalog (see TodayPriceCatalogController for management).
    /// </summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayPrices([FromQuery] string? region = null, [FromQuery] string? grade = "A")
    {
        var result = await _catalogService.GetLivePrices(region, grade);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/prices/estimate — Quick AI estimate for farmer when selecting crop & region in listing form
    /// </summary>
    [HttpGet("estimate")]
    public async Task<IActionResult> GetQuickPriceEstimate(
        [FromQuery] string cropId,
        [FromQuery] string regionId,
        [FromQuery] string? cropName = null,
        [FromQuery] string? regionName = null,
        [FromQuery] decimal quantity = 100m,
        [FromQuery] string grade = "A")
    {
        var result = await _agenticAi.EstimateFairPriceAsync(
            cropId: cropId,
            regionId: regionId,
            quantity: quantity,
            claimedGrade: grade,
            cropName: cropName,
            regionName: regionName
        );
        return Ok(result);
    }

    /// <summary>
    /// POST /api/prices/estimate — Post body version for complex recent sale data
    /// </summary>
    [HttpPost("estimate")]
    public async Task<IActionResult> PostPriceEstimate([FromBody] EstimatePriceRequestDto req)
    {
        var result = await _agenticAi.EstimateFairPriceAsync(
            cropId: req.CropId,
            regionId: req.RegionId,
            quantity: req.Quantity,
            claimedGrade: req.ClaimedGrade,
            cropName: req.CropName,
            regionName: req.RegionName
        );
        return Ok(result);
    }

    /// <summary>
    /// POST /api/prices/orchestrate — Runs the LangGraph StateGraph coordinator workflow
    /// </summary>
    [HttpPost("orchestrate")]
    public async Task<IActionResult> RunOrchestration([FromBody] OrchestrationRequestDto req)
    {
        var jsonResponse = await _agenticAi.RunOrchestrationAsync(req);
        return Content(jsonResponse, "application/json");
    }
}
