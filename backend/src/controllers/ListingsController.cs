using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

[ApiController]
[Route("api/listings")]
[Authorize]
public class ListingsController : ControllerBase
{
    private readonly ListingService _service;

    public ListingsController(ListingService service)
    {
        _service = service;
    }

    /// <summary>
    /// Reads the authenticated user's ID from the validated JWT. [Authorize]
    /// on the controller guarantees this is never called for an anonymous
    /// request, so there is no default/demo-user fallback here.
    /// </summary>
    private Guid GetCurrentUserId() => AuthService.GetUser(User)!.Value.UserId;

    private string GetCurrentUserRole() => AuthService.GetUser(User)!.Value.Role;

    /// <summary>
    /// POST /api/listings — Create a new produce listing (FR3). Farmer-only —
    /// this endpoint had no role gate at all until a full-system integration
    /// audit (2026-09-27) found a Buyer/Officer/Administrator could create a
    /// listing attributed to themselves. See PROGRESS.md.
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingDto dto)
    {
        try
        {
            var farmerId = GetCurrentUserId();
            var result = await _service.CreateListing(farmerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/listings — Search, filter, sort, paginate listings (FR6).
    /// Buyer/Farmer marketplace browse is forced to Published regardless of
    /// what the caller requests — this endpoint previously applied no default
    /// status filter at all, so a Buyer omitting ?status (or explicitly
    /// requesting a non-Published one) saw PendingApproval/Withdrawn/Rejected
    /// listings too. Found during a full-system integration audit
    /// (2026-09-27), confirmed exploitable via a plain GET with no query
    /// params. Officer/Administrator are unaffected — the Admin Dashboard's
    /// review queue genuinely needs to see PendingApproval listings, and both
    /// roles are already trusted to see the full catalogue. See PROGRESS.md.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ListingSearchQuery query)
    {
        var role = GetCurrentUserRole();

        if (role == Roles.Farmer)
        {
            // Exclude the caller's own listings from marketplace results.
            query.ExcludeFarmerId = GetCurrentUserId();
        }

        if (role == Roles.Buyer || role == Roles.Farmer)
        {
            query.Status = "Published";
        }

        var result = await _service.GetListings(query);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/listings/my — Get current farmer's own listings
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpGet("my")]
    public async Task<IActionResult> MyListings([FromQuery] ListingSearchQuery query)
    {
        var farmerId = GetCurrentUserId();
        var result = await _service.GetListingsByFarmer(farmerId, query);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/listings/{id} — Get listing detail
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetListingById(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
    }

    /// <summary>
    /// PUT /api/listings/{id} — Edit a listing not yet ordered against (FR7)
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateListingDto dto)
    {
        try
        {
            var farmerId = GetCurrentUserId();
            var result = await _service.UpdateListing(id, farmerId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/listings/{id} — Withdraw a listing (FR7)
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        try
        {
            var farmerId = GetCurrentUserId();
            await _service.WithdrawListing(id, farmerId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PATCH /api/listings/{id}/approve was removed during Component C integration
    // (2026-09-27): it set Status = Published directly with no quality check at
    // all, bypassing FR5's inspection gate entirely. The only path to Published
    // now is POST /api/listings/{id}/publish (ListingsInspectionController),
    // which requires a passed inspection first. See PROGRESS.md, Decisions.

    /// <summary>
    /// PATCH /api/listings/{id}/reject — Admin rejects a listing
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            var result = await _service.RejectListing(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PATCH /api/listings/{id}/price-suggestion/approve — Officer/Admin approves
    /// the AI-suggested fair price as-is. This is the human-approval checkpoint
    /// the agentic AI workflow pauses at — no price becomes final without it.
    /// </summary>
    [Authorize(Roles = Roles.OfficerAdmin)]
    [HttpPatch("{id}/price-suggestion/approve")]
    public async Task<IActionResult> ApprovePriceSuggestion(Guid id)
    {
        try
        {
            var result = await _service.DecidePriceSuggestion(id, "Approved", GetCurrentUserId(), null);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing or price suggestion not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PATCH /api/listings/{id}/price-suggestion/reject — Officer/Admin rejects
    /// the AI-suggested fair price outright.
    /// </summary>
    [Authorize(Roles = Roles.OfficerAdmin)]
    [HttpPatch("{id}/price-suggestion/reject")]
    public async Task<IActionResult> RejectPriceSuggestion(Guid id, [FromBody] DecidePriceSuggestionDto? dto)
    {
        try
        {
            var result = await _service.DecidePriceSuggestion(id, "Rejected", GetCurrentUserId(), dto?.OfficerNote);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing or price suggestion not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PATCH /api/listings/{id}/price-suggestion/revise — Officer/Admin revises
    /// the suggested range before it becomes final (FR: "Approve / Reject / Request Revision").
    /// </summary>
    [Authorize(Roles = Roles.OfficerAdmin)]
    [HttpPatch("{id}/price-suggestion/revise")]
    public async Task<IActionResult> RevisePriceSuggestion(Guid id, [FromBody] RevisePriceSuggestionDto dto)
    {
        try
        {
            var result = await _service.RevisePriceSuggestion(id, GetCurrentUserId(), dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing or price suggestion not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/listings/{id}/photos — Add photos to a listing
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpPost("{id}/photos")]
    public async Task<IActionResult> AddPhotos(Guid id, [FromBody] AddPhotosDto dto)
    {
        try
        {
            var farmerId = GetCurrentUserId();
            var result = await _service.AddPhotos(id, farmerId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// GET /api/listings/{id}/price-suggestion — Get/trigger AI fair-price suggestion (FR4)
    /// </summary>
    [HttpGet("{id}/price-suggestion")]
    public async Task<IActionResult> GetPriceSuggestion(Guid id)
    {
        try
        {
            var result = await _service.GetPriceSuggestion(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Listing not found." });
        }
    }
}
