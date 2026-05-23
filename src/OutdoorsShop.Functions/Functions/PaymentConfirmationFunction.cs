using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Infrastructure.Data;
using System.Text.Json;

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
    /// Triggered by messages on the 'payment-results' queue.
    /// Updates SalesOrder.PaymentStatus based on the payment result.
    /// </summary>
    [Function("PaymentConfirmation")]
    public async Task Run([QueueTrigger("payment-results")] string queueMessage)
    {
        _logger.LogInformation("PaymentConfirmation triggered. Message: {Message}", queueMessage);

        PaymentResultMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<PaymentResultMessage>(queueMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment result message.");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received null payment result message.");
            return;
        }

        var order = await _dbContext.Orders.FindAsync(message.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment update.", message.OrderId);
            return;
        }

        order.PaymentStatus = message.Success ? PaymentStatus.Confirmed : PaymentStatus.Failed;

        if (message.Success)
            order.Status = OrderStatus.Processing;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} PaymentStatus updated to {PaymentStatus}.", order.OrderID, order.PaymentStatus);
    }
}

public record PaymentResultMessage(int OrderId, bool Success);
