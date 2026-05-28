using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using System.Text.Json;

namespace OutdoorsShop.Functions.Functions;

public class ReportExportFunction
{
    private readonly IReportExportRequestService _reportExportRequestService;
    private readonly ILogger<ReportExportFunction> _logger;

    public ReportExportFunction(
        IReportExportRequestService reportExportRequestService,
        ILogger<ReportExportFunction> logger)
    {
        _reportExportRequestService = reportExportRequestService;
        _logger = logger;
    }

    [Function("ReportExport")]
    public async Task Run([QueueTrigger("report-export-requests", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        ReportExportRequestMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ReportExportRequestMessage>(
                queueMessage,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize report export request message.");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Received null report export request message.");
            return;
        }

        await _reportExportRequestService.ProcessAsync(message.RequestId);
    }
}
