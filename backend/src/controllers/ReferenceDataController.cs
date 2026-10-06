using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgriConnect.Api.Config;

namespace AgriConnect.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = Roles.BuyerFarmerOfficerAdmin)]
public class ReferenceDataController : ControllerBase
{
    private readonly ListingService _service;

    public ReferenceDataController(ListingService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/crops — Get all crop types (reference data for dropdowns)
    /// </summary>
    [HttpGet("crops")]
    public async Task<IActionResult> GetCrops()
    {
        var result = await _service.GetAllCrops();
        return Ok(result);
    }

    /// <summary>
    /// GET /api/regions — Get all regions (reference data for dropdowns)
    /// </summary>
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions()
    {
        var result = await _service.GetAllRegions();
        return Ok(result);
    }
}
