using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize(Roles = "Administrator")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryController(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Get all inventory records (Admin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _inventoryRepository.GetAllAsync();
        var dtos = items.Select(i => new InventoryDto
        {
            ProductID = i.ProductID,
            ProductName = i.Product?.Name ?? string.Empty,
            QuantityAvailable = i.QuantityAvailable,
            LastUpdated = i.LastUpdated,
            ReorderThreshold = i.ReorderThreshold
        });
        return Ok(dtos);
    }

    /// <summary>Update inventory quantity for a product (Admin only).</summary>
    [HttpPatch("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] UpdateInventoryDto dto)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory is null)
            return NotFound();

        inventory.QuantityAvailable = dto.QuantityAvailable;
        inventory.ReorderThreshold = dto.ReorderThreshold;
        inventory.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpdateAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        return NoContent();
    }
}

public record UpdateInventoryDto(int QuantityAvailable, int ReorderThreshold);
