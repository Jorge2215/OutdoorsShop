using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    /// Adjusts ProductInventory.QuantityAvailable and logs a reorder alert if stock is low.
    /// </summary>
    [Function("StockUpdate")]
    public async Task Run([QueueTrigger("stock-updates")] string queueMessage)
    {
        _logger.LogInformation("StockUpdate triggered. Message: {Message}", queueMessage);

        StockUpdateMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<StockUpdateMessage>(queueMessage);
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

        var inventory = await _dbContext.Inventory.FindAsync(message.ProductId);
        if (inventory is null)
        {
            _logger.LogWarning("Inventory record for Product {ProductId} not found.", message.ProductId);
            return;
        }

        inventory.QuantityAvailable += message.QuantityDelta;
        inventory.LastUpdated = DateTime.UtcNow;

        if (inventory.QuantityAvailable < 0)
            inventory.QuantityAvailable = 0;

        if (inventory.QuantityAvailable <= inventory.ReorderThreshold)
        {
            _logger.LogWarning(
                "LOW STOCK ALERT: Product {ProductId} has {Quantity} units available (threshold: {Threshold}).",
                message.ProductId, inventory.QuantityAvailable, inventory.ReorderThreshold);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Inventory updated for Product {ProductId}. New quantity: {Quantity}.",
            message.ProductId, inventory.QuantityAvailable);
    }
}

public record StockUpdateMessage(int ProductId, int QuantityDelta);
