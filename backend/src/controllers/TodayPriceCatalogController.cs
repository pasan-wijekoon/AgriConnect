using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// Admin-only management of the "Today's Prices" catalog — which crops
/// appear on the marketplace discovery page. Prices themselves are never
/// stored here; they're computed live by the Fair-Price Estimation Agent.
/// </summary>
[ApiController]
[Route("api/admin/today-prices-catalog")]
[Authorize(Roles = "Admin")]
public class TodayPriceCatalogController : ControllerBase
{
    private readonly TodayPriceCatalogService _service;

    public TodayPriceCatalogController(TodayPriceCatalogService service)
    {
        _service = service;
    }

    /// <summary>GET /api/admin/today-prices-catalog — list all catalog items (active & inactive)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result);
    }

    /// <summary>POST /api/admin/today-prices-catalog — add a new crop to the Today's Prices page</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodayPriceCatalogItemDto dto)
    {
        var result = await _service.Create(dto);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    /// <summary>PUT /api/admin/today-prices-catalog/{id} — edit a catalog item</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodayPriceCatalogItemDto dto)
    {
        try
        {
            var result = await _service.Update(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Catalog item not found." });
        }
    }

    /// <summary>DELETE /api/admin/today-prices-catalog/{id} — remove a crop from the Today's Prices page</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.Delete(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Catalog item not found." });
        }
    }
}
