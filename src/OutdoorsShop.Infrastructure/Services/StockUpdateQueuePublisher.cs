using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using System.Text.Json;

namespace OutdoorsShop.Infrastructure.Services;

public class StockUpdateQueuePublisher : IStockUpdateQueuePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<StockUpdateQueuePublisher> _logger;
    private readonly string _connectionString;
    private readonly string _queueName;

    public StockUpdateQueuePublisher(IConfiguration configuration, ILogger<StockUpdateQueuePublisher> logger)
    {
        _logger = logger;
        _connectionString = configuration["AzureStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"]
            ?? string.Empty;
        _queueName = configuration["AzureStorage:StockUpdatesQueueName"]
            ?? "stock-updates";
    }

    public async Task EnqueueAsync(StockUpdateMessage message, CancellationToken cancellationToken = default)
    {
        var queueClient = new QueueClient(
            string.IsNullOrWhiteSpace(_connectionString) ? "UseDevelopmentStorage=true" : _connectionString,
            _queueName,
            new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        await queueClient.SendMessageAsync(payload, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Queued stock update for product {ProductId} with delta {Delta} to {QueueName}.",
            message.ProductId,
            message.QuantityDelta,
            _queueName);
    }
}
