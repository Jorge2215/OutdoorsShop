using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PagedResult<InventoryDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _inventoryRepository.GetPagedAsync(normalizedPageNumber, normalizedPageSize);

        return new PagedResult<InventoryDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult<InventoryDto>> GetByProductIdAsync(int productId)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory is null)
            return OperationResult<InventoryDto>.NotFoundResult($"Inventory for product {productId} not found.");

        return OperationResult<InventoryDto>.Success(MapToDto(inventory));
    }

    public async Task<OperationResult<InventoryDto>> UpdateAsync(int productId, UpdateInventoryDto request)
    {
        if (request.QuantityAvailable is null && request.ReorderThreshold is null)
            return OperationResult<InventoryDto>.Invalid("Provide QuantityAvailable and/or ReorderThreshold.");

        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory is null)
            return OperationResult<InventoryDto>.NotFoundResult($"Inventory for product {productId} not found.");

        if (request.QuantityAvailable.HasValue)
            inventory.QuantityAvailable = request.QuantityAvailable.Value;

        if (request.ReorderThreshold.HasValue)
            inventory.ReorderThreshold = request.ReorderThreshold.Value;

        inventory.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpdateAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        return OperationResult<InventoryDto>.Success(MapToDto(inventory));
    }

    public async Task<IReadOnlyList<InventoryDto>> GetLowStockAsync()
    {
        var items = await _inventoryRepository.GetLowStockAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<InventoryReportRowDto>> GetReportRowsAsync()
    {
        var items = await _inventoryRepository.GetAllAsync();
        return items.Select(item => new InventoryReportRowDto
        {
            ProductID = item.ProductID,
            ProductName = item.Product?.Name ?? string.Empty,
            QuantityAvailable = item.QuantityAvailable,
            ReorderThreshold = item.ReorderThreshold,
            LastUpdated = item.LastUpdated,
            IsLowStock = item.QuantityAvailable <= item.ReorderThreshold
        }).ToList();
    }

    private static InventoryDto MapToDto(ProductInventory inventory) => new()
    {
        ProductID = inventory.ProductID,
        ProductName = inventory.Product?.Name ?? string.Empty,
        QuantityAvailable = inventory.QuantityAvailable,
        LastUpdated = inventory.LastUpdated,
        ReorderThreshold = inventory.ReorderThreshold
    };
}
