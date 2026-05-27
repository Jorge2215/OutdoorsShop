using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace OutdoorsShop.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly AppDbContext _dbContext;
    private readonly IStockUpdateQueuePublisher _stockUpdateQueuePublisher;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        AppDbContext dbContext,
        IStockUpdateQueuePublisher stockUpdateQueuePublisher,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _dbContext = dbContext;
        _stockUpdateQueuePublisher = stockUpdateQueuePublisher;
        _logger = logger;
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

        StockUpdateMessage? stockUpdateMessage = null;
        var updatedAt = DateTimeOffset.UtcNow;

        if (request.QuantityAvailable.HasValue)
        {
            var quantityDelta = request.QuantityAvailable.Value - inventory.QuantityAvailable;
            if (quantityDelta != 0)
            {
                inventory.QuantityAvailable = request.QuantityAvailable.Value;
                stockUpdateMessage = new StockUpdateMessage(
                    ProductId: productId,
                    QuantityDelta: quantityDelta,
                    Reason: "AdminAdjustment",
                    Notes: "Admin inventory quantity update",
                    UpdatedAt: updatedAt);

                _dbContext.StockUpdateLogs.Add(new StockUpdateLog
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    QuantityDelta = quantityDelta,
                    ResultingQuantity = inventory.QuantityAvailable,
                    Reason = stockUpdateMessage.Reason,
                    Notes = stockUpdateMessage.Notes,
                    UpdatedAt = stockUpdateMessage.UpdatedAt
                });
            }
        }

        if (request.ReorderThreshold.HasValue)
            inventory.ReorderThreshold = request.ReorderThreshold.Value;

        inventory.LastUpdated = updatedAt.UtcDateTime;

        await _inventoryRepository.UpdateAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        if (stockUpdateMessage is not null)
        {
            try
            {
                await _stockUpdateQueuePublisher.EnqueueAsync(stockUpdateMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Inventory for product {ProductId} was updated but the stock update queue publish failed.",
                    productId);
            }
        }

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
