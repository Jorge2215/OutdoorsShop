using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OutdoorsShop.Functions.Functions;

public class ReceiptGenerationFunction
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly string _receiptsContainerName;
    private readonly ILogger<ReceiptGenerationFunction> _logger;

    public ReceiptGenerationFunction(
        AppDbContext dbContext,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        ILogger<ReceiptGenerationFunction> logger)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _receiptsContainerName = configuration["AzureStorage:ReceiptsContainer"]
            ?? OrderReceiptStorageConventions.DefaultContainerName;
        _logger = logger;
    }

    [Function("ReceiptGeneration")]
    public async Task Run([QueueTrigger("receipt-requests", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        ReceiptGenerationMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ReceiptGenerationMessage>(
                queueMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize receipt generation message.");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received null receipt generation message.");
            return;
        }

        var order = await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.OrderID == message.OrderId);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for receipt generation.", message.OrderId);
            return;
        }

        if (order.PaymentStatus != PaymentStatus.Confirmed)
        {
            _logger.LogWarning(
                "Order {OrderId} is not payment confirmed. Receipt generation skipped.",
                order.OrderID);
            return;
        }

        var blobName = string.IsNullOrWhiteSpace(message.ReceiptBlobName)
            ? OrderReceiptStorageConventions.GetBlobName(order.OrderID)
            : message.ReceiptBlobName;

        var html = BuildReceiptHtml(order, message);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        await _blobStorageService.DeleteAsync(_receiptsContainerName, blobName);
        await _blobStorageService.UploadAsync(_receiptsContainerName, blobName, stream, "text/html; charset=utf-8");

        _logger.LogInformation(
            "Receipt for order {OrderId} uploaded to {Container}/{BlobName}.",
            order.OrderID,
            _receiptsContainerName,
            blobName);
    }

    private static string BuildReceiptHtml(SalesOrder order, ReceiptGenerationMessage message)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine($"  <title>Receipt #{order.OrderID}</title>");
        builder.AppendLine("  <style>body{font-family:Arial,sans-serif;margin:24px;color:#1f2937;}table{width:100%;border-collapse:collapse;margin-top:16px;}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;}th{background:#f3f4f6;}h1{margin-bottom:4px;}p{margin:4px 0;}</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine($"  <h1>Order Receipt #{order.OrderID}</h1>");
        builder.AppendLine($"  <p><strong>Customer:</strong> {HtmlEncode(order.Customer.Name)}</p>");
        builder.AppendLine($"  <p><strong>Email:</strong> {HtmlEncode(order.Customer.Email)}</p>");
        builder.AppendLine($"  <p><strong>Order Date:</strong> {order.OrderDate:yyyy-MM-dd HH:mm:ss} UTC</p>");
        builder.AppendLine($"  <p><strong>Payment Confirmed:</strong> {message.ConfirmedAt.UtcDateTime:yyyy-MM-dd HH:mm:ss} UTC</p>");
        builder.AppendLine($"  <p><strong>Payment Reference:</strong> {HtmlEncode(message.PaymentReference)}</p>");
        builder.AppendLine($"  <p><strong>Shipping Address:</strong> {HtmlEncode(order.ShippingAddress)}</p>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Product</th><th>Quantity</th><th>Unit Price</th><th>Line Total</th></tr></thead>");
        builder.AppendLine("    <tbody>");

        foreach (var detail in order.Details)
        {
            var productName = detail.Product?.Name ?? $"Product {detail.ProductID}";
            builder.AppendLine(
                $"      <tr><td>{HtmlEncode(productName)}</td><td>{detail.Quantity}</td><td>{FormatCurrency(detail.UnitPrice)}</td><td>{FormatCurrency(detail.Quantity * detail.UnitPrice)}</td></tr>");
        }

        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");
        builder.AppendLine($"  <p><strong>Total:</strong> {FormatCurrency(order.TotalAmount)}</p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatCurrency(decimal amount)
        => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
