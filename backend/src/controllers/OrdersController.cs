using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// FR8 (place order), FR9 (reservation, via OrderService/StockReservationService),
/// FR10 (scheduling, via SchedulingService), FR11 (status tracking).
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(OrderService orderService, SchedulingService schedulingService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Buyer)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.PlaceOrderAsync(User.GetUserId(), request, ct);
        if (!result.Success)
        {
            return ToErrorResult(result.Error, result.ErrorMessage!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [Authorize(Roles = Roles.BuyerFarmerOfficer)]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var result = await orderService.ListAsync(User.GetUserId(), User.GetRole(), status, page, size, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.BuyerFarmerOfficer)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await orderService.GetByIdAsync(id, User.GetUserId(), User.GetRole(), ct);
        return result.Success ? Ok(result.Value) : ToErrorResult(result.Error, result.ErrorMessage!);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = Roles.Officer)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderStatusUpdateRequest request, CancellationToken ct)
    {
        var result = await orderService.UpdateStatusAsync(id, request.Status, User.GetUserId(), ct);
        return result.Success ? Ok(result.Value) : ToErrorResult(result.Error, result.ErrorMessage!);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Roles.BuyerOfficer)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] OrderCancelRequest? request, CancellationToken ct)
    {
        var result = await orderService.CancelAsync(id, User.GetUserId(), User.GetRole(), request?.Reason, ct);
        return result.Success ? Ok(result.Value) : ToErrorResult(result.Error, result.ErrorMessage!);
    }

    /// <summary>
    /// Read-only schedule lookup for an order (404 if none exists yet). Not in
    /// the plan's original endpoint table — added so a UI can display current
    /// schedule state without triggering ProposeAsync's side effects. Reuses
    /// OrderService's ownership/visibility check first (same IDOR protection
    /// as GetById, plan §6) before ever touching the schedule.
    /// </summary>
    [HttpGet("{id:guid}/schedule")]
    [Authorize(Roles = Roles.BuyerFarmerOfficer)]
    [ProducesResponseType(typeof(ScheduleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(Guid id, CancellationToken ct)
    {
        var orderVisibility = await orderService.GetByIdAsync(id, User.GetUserId(), User.GetRole(), ct);
        if (!orderVisibility.Success)
        {
            return ToErrorResult(orderVisibility.Error, orderVisibility.ErrorMessage!);
        }

        var result = await schedulingService.GetByOrderIdAsync(id, ct);
        return result.Success ? Ok(result.Value) : ToSchedulingErrorResult(result.Error, result.ErrorMessage!);
    }

    /// <summary>
    /// FR10 — propose a conflict-free pickup/delivery slot. "System (internal,
    /// triggered post-Approval) / Officer (manual re-trigger)" per plan §5.2; there
    /// is no internal-service-to-service caller in this codebase yet, so this is
    /// Officer-triggered only for now (documented simplification, PROGRESS.md).
    /// Also serves as the "Revise" action (plan §8.1) — call again with a new
    /// preferredWindow to replace an existing Proposed schedule.
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    [Authorize(Roles = Roles.Officer)]
    [ProducesResponseType(typeof(ScheduleResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ProposeSchedule(Guid id, [FromBody] CreateScheduleRequest request, CancellationToken ct)
    {
        var result = await schedulingService.ProposeAsync(id, request, User.GetUserId(), ct);
        if (!result.Success)
        {
            return ToSchedulingErrorResult(result.Error, result.ErrorMessage!);
        }

        return AcceptedAtAction(nameof(GetById), new { id }, result.Value);
    }

    /// <summary>
    /// FR19 — Officer Approve/Reject decision on the order's pending schedule
    /// proposal. No generic AgentWorkflow controller exists anywhere in the repo
    /// (confirmed by searching all branches), so per the plan's own stated default
    /// (§16 open question #4) this is Component B's minimal, self-scoped decision
    /// route rather than a shared one. Never auto-confirms — this endpoint is the
    /// only path that can move a PickupSchedule to Confirmed (CLAUDE.md §17/§18).
    /// </summary>
    [HttpPut("{id:guid}/schedule/decision")]
    [Authorize(Roles = Roles.Officer)]
    [ProducesResponseType(typeof(ScheduleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DecideSchedule(Guid id, [FromBody] ScheduleDecisionRequest request, CancellationToken ct)
    {
        var result = await schedulingService.DecideAsync(id, request.Decision, User.GetUserId(), ct);
        return result.Success ? Ok(result.Value) : ToSchedulingErrorResult(result.Error, result.ErrorMessage!);
    }

    private ObjectResult ToErrorResult(OrderOperationError error, string message) => error switch
    {
        OrderOperationError.NotFound => Problem(detail: message, statusCode: StatusCodes.Status404NotFound),
        OrderOperationError.Forbidden => Problem(detail: message, statusCode: StatusCodes.Status403Forbidden),
        OrderOperationError.Conflict => Problem(detail: message, statusCode: StatusCodes.Status409Conflict),
        OrderOperationError.InvalidRequest => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest),
        _ => Problem(detail: message, statusCode: StatusCodes.Status500InternalServerError)
    };

    private ObjectResult ToSchedulingErrorResult(SchedulingOperationError error, string message) => error switch
    {
        SchedulingOperationError.NotFound => Problem(detail: message, statusCode: StatusCodes.Status404NotFound),
        SchedulingOperationError.Conflict => Problem(detail: message, statusCode: StatusCodes.Status409Conflict),
        SchedulingOperationError.InvalidRequest => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest),
        _ => Problem(detail: message, statusCode: StatusCodes.Status500InternalServerError)
    };
}
