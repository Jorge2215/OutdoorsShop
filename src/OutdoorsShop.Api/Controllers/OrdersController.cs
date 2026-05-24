using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null)
    {
        var result = await _orderService.GetPagedAsync(pageNumber, pageSize, status, User.IsInRole("Administrator"), GetCurrentCustomerId());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orderService.GetByIdAsync(id, User.IsInRole("Administrator"), GetCurrentCustomerId());
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var currentCustomerId = GetCurrentCustomerId();
        if (!currentCustomerId.HasValue)
            return BadRequest(new { message = "Authenticated customer_id claim is missing." });

        var result = await _orderService.CreateAsync(currentCustomerId.Value, request);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Order could not be created." });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.OrderID }, result.Value);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _orderService.CancelAsync(id);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage ?? "Order could not be cancelled." });

        return NoContent();
    }

    private int? GetCurrentCustomerId()
        => int.TryParse(User.FindFirstValue("customer_id"), out var customerId) ? customerId : null;

    private IActionResult ToActionResult(OperationResult<OrderDto> result)
    {
        if (result.Forbidden)
            return Forbid();

        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Request could not be completed." });

        return Ok(result.Value);
    }
}
