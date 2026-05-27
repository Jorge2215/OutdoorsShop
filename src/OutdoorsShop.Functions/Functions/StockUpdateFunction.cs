using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using System.Text.Json;

namespace OutdoorsShop.Functions.Functions;

public class StockUpdateFunction
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StockUpdateFunction> _logger;

    public StockUpdateFunction(AppDbContext dbContext, ILogger<StockUpdateFunction> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Triggered by messages on the 'stock-updates' queue.
    /// Adjusts ProductInventory.QuantityAvailable, clamps to 0, logs movement, and warns on low stock.
    /// </summary>
    [Function("StockUpdate")]
    public async Task Run([QueueTrigger("stock-updates", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("StockUpdate triggered. Message length: {Length}", queueMessage.Length);

        StockUpdateMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<StockUpdateMessage>(queueMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize stock update message.");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received null stock update message.");
            return;
        }

        var existingLog = await _dbContext.StockUpdateLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log =>
                log.ProductId == message.ProductId &&
                log.QuantityDelta == message.QuantityDelta &&
                log.Reason == message.Reason &&
                log.Notes == message.Notes &&
                log.UpdatedAt == message.UpdatedAt);

        if (existingLog is not null)
        {
            _logger.LogInformation(
                "Stock update message for Product {ProductId} at {UpdatedAt} was already applied. Skipping duplicate.",
                message.ProductId,
                message.UpdatedAt);
            return;
        }

        var inventory = await _dbContext.Inventory.FindAsync(message.ProductId);

        if (inventory is null)
        {
            _logger.LogInformation(
                "No inventory record for Product {ProductId}. Creating one.", message.ProductId);
            inventory = new ProductInventory
            {
                ProductID = message.ProductId,
                QuantityAvailable = 0,
                ReorderThreshold = 5,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.Inventory.Add(inventory);
        }

        var previousQty = inventory.QuantityAvailable;
        inventory.QuantityAvailable = Math.Max(0, inventory.QuantityAvailable + message.QuantityDelta);
        inventory.LastUpdated = DateTime.UtcNow;

        var log = new StockUpdateLog
        {
            Id = Guid.NewGuid(),
            ProductId = message.ProductId,
            QuantityDelta = message.QuantityDelta,
            ResultingQuantity = inventory.QuantityAvailable,
            Reason = message.Reason,
            Notes = message.Notes,
            UpdatedAt = message.UpdatedAt
        };
        _dbContext.StockUpdateLogs.Add(log);

        if (inventory.QuantityAvailable <= inventory.ReorderThreshold)
        {
            _logger.LogWarning(
                "⚠️ Low stock alert: Product {ProductId}, quantity {Qty} (threshold: {Threshold})",
                message.ProductId, inventory.QuantityAvailable, inventory.ReorderThreshold);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Stock updated for Product {ProductId}. {PreviousQty} → {NewQty} (delta: {Delta}, reason: {Reason})",
            message.ProductId, previousQty, inventory.QuantityAvailable, message.QuantityDelta, message.Reason);
    }
}
