using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// FR8 (place order), FR9 (reservation, via OrderService/StockReservationService),
/// FR11 (status tracking). Scheduling endpoints (FR10) land in Phase 6.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(OrderService orderService) : ControllerBase
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
        var result = await orderService.UpdateStatusAsync(id, request.Status, ct);
        return result.Success ? Ok(result.Value) : ToErrorResult(result.Error, result.ErrorMessage!);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Roles.BuyerOfficer)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] OrderCancelRequest? request, CancellationToken ct)
    {
        var result = await orderService.CancelAsync(id, User.GetUserId(), User.GetRole(), ct);
        return result.Success ? Ok(result.Value) : ToErrorResult(result.Error, result.ErrorMessage!);
    }

    private ObjectResult ToErrorResult(OrderOperationError error, string message) => error switch
    {
        OrderOperationError.NotFound => Problem(detail: message, statusCode: StatusCodes.Status404NotFound),
        OrderOperationError.Forbidden => Problem(detail: message, statusCode: StatusCodes.Status403Forbidden),
        OrderOperationError.Conflict => Problem(detail: message, statusCode: StatusCodes.Status409Conflict),
        OrderOperationError.InvalidRequest => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest),
        _ => Problem(detail: message, statusCode: StatusCodes.Status500InternalServerError)
    };
}
