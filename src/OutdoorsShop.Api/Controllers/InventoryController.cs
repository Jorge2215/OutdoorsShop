using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Administrator")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _inventoryService.GetPagedAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock()
    {
        var result = await _inventoryService.GetLowStockAsync();
        return Ok(result);
    }

    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var result = await _inventoryService.GetByProductIdAsync(productId);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Value);
    }

    [HttpPut("{productId:int}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int productId, [FromBody] UpdateInventoryDto request)
    {
        var result = await _inventoryService.UpdateAsync(productId, request);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Inventory could not be updated." });

        return Ok(result.Value);
    }
}
