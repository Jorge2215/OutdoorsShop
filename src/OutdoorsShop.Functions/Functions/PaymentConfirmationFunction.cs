using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Infrastructure.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutdoorsShop.Functions.Functions;

public class PaymentConfirmationFunction
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PaymentConfirmationFunction> _logger;

    public PaymentConfirmationFunction(AppDbContext dbContext, ILogger<PaymentConfirmationFunction> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Triggered by messages on the 'payment-confirmations' queue.
    /// Updates order status and payment fields; restores inventory on payment failure.
    /// </summary>
    [Function("PaymentConfirmation")]
    public async Task Run([QueueTrigger("payment-confirmations", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("PaymentConfirmation triggered. Message length: {Length}", queueMessage.Length);

        PaymentConfirmationMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<PaymentConfirmationMessage>(queueMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment confirmation message.");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received null payment confirmation message.");
            return;
        }

        var order = await _dbContext.Orders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrderID == message.OrderId);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment confirmation.", message.OrderId);
            return;
        }

        switch (message.PaymentStatus)
        {
            case "Success":
                order.Status = OrderStatus.Processing;
                order.PaymentStatus = PaymentStatus.Confirmed;
                order.PaymentReference = message.PaymentReference;
                order.PaidAt = message.ProcessedAt;
                _logger.LogInformation(
                    "Order {OrderId} confirmed. Reference: {Ref}, Amount: {Amount}",
                    order.OrderID, message.PaymentReference, message.Amount);
                break;

            case "Failed":
                order.Status = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Failed;
                await RestoreInventoryAsync(order.Details);
                _logger.LogInformation("Order {OrderId} cancelled due to payment failure.", order.OrderID);
                break;

            case "Pending":
                _logger.LogInformation(
                    "Order {OrderId} payment still pending. No action taken (would re-enqueue).",
                    order.OrderID);
                return;

            default:
                _logger.LogWarning(
                    "Unknown paymentStatus '{Status}' for Order {OrderId}.",
                    message.PaymentStatus, order.OrderID);
                return;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "Order {OrderId} saved with status {Status}.", order.OrderID, order.Status);
    }

    private async Task RestoreInventoryAsync(IEnumerable<Core.Entities.SalesOrderDetail> details)
    {
        foreach (var detail in details)
        {
            var inventory = await _dbContext.Inventory.FindAsync(detail.ProductID);
            if (inventory is not null)
            {
                inventory.QuantityAvailable += detail.Quantity;
                inventory.LastUpdated = DateTime.UtcNow;
                _logger.LogInformation(
                    "Restored {Qty} units to Product {ProductId}. New stock: {Stock}",
                    detail.Quantity, detail.ProductID, inventory.QuantityAvailable);
            }
            else
            {
                _logger.LogWarning(
                    "No inventory record found for Product {ProductId} during restore.",
                    detail.ProductID);
            }
        }
    }
}

public record PaymentConfirmationMessage(
    [property: JsonPropertyName("orderId")] int OrderId,
    [property: JsonPropertyName("paymentReference")] string PaymentReference,
    [property: JsonPropertyName("paymentStatus")] string PaymentStatus,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("processedAt")] DateTimeOffset ProcessedAt
);
