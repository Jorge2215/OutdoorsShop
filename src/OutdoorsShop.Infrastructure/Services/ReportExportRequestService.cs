using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Services;

public class ReportExportRequestService : IReportExportRequestService
{
    private static readonly string[] SupportedReportTypes = ["orders", "inventory"];
    private static readonly string[] SupportedFormats = ["csv", "excel"];
    private readonly AppDbContext _dbContext;
    private readonly IReportFileService _reportFileService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IReportExportQueuePublisher _reportExportQueuePublisher;
    private readonly ILogger<ReportExportRequestService> _logger;
    private readonly string _containerName;

    public ReportExportRequestService(
        AppDbContext dbContext,
        IReportFileService reportFileService,
        IBlobStorageService blobStorageService,
        IReportExportQueuePublisher reportExportQueuePublisher,
        IConfiguration configuration,
        ILogger<ReportExportRequestService> logger)
    {
        _dbContext = dbContext;
        _reportFileService = reportFileService;
        _blobStorageService = blobStorageService;
        _reportExportQueuePublisher = reportExportQueuePublisher;
        _logger = logger;
        _containerName = configuration["AzureStorage:ReportExportsContainer"]
            ?? ReportExportStorageConventions.DefaultContainerName;
    }

    public async Task<OperationResult<ReportExportRequestDto>> CreateAsync(ReportExportRequestCreateDto request, string? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var normalizedReportType = request.ReportType.Trim().ToLowerInvariant();
        var normalizedFormat = request.Format.Trim().ToLowerInvariant();

        if (!SupportedReportTypes.Contains(normalizedReportType))
            return OperationResult<ReportExportRequestDto>.Invalid("Supported report types are orders and inventory.");

        if (!SupportedFormats.Contains(normalizedFormat))
            return OperationResult<ReportExportRequestDto>.Invalid("Supported formats are csv and excel.");

        if (request.From.HasValue && request.To.HasValue && request.From > request.To)
            return OperationResult<ReportExportRequestDto>.Invalid("The 'from' date must be earlier than or equal to the 'to' date.");

        var exportRequest = new ReportExportRequest
        {
            Id = Guid.NewGuid(),
            ReportType = normalizedReportType,
            Format = normalizedFormat,
            Status = ReportExportRequestStatuses.Pending,
            From = request.From,
            To = request.To,
            RequestedAt = DateTimeOffset.UtcNow,
            RequestedByUserId = requestedByUserId
        };

        _dbContext.ReportExportRequests.Add(exportRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _reportExportQueuePublisher.EnqueueAsync(new ReportExportRequestMessage(exportRequest.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            exportRequest.Status = ReportExportRequestStatuses.Failed;
            exportRequest.FailedAt = DateTimeOffset.UtcNow;
            exportRequest.ErrorMessage = TruncateErrorMessage(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to queue report export request {RequestId}.", exportRequest.Id);
            throw;
        }

        return OperationResult<ReportExportRequestDto>.Success(MapToDto(exportRequest));
    }

    public async Task<OperationResult<ReportExportRequestDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exportRequest = await _dbContext.ReportExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (exportRequest is null)
            return OperationResult<ReportExportRequestDto>.NotFoundResult($"Report export request {id} was not found.");

        return OperationResult<ReportExportRequestDto>.Success(MapToDto(exportRequest));
    }

    public async Task<OperationResult<ReportExportDownloadDto>> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exportRequest = await _dbContext.ReportExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (exportRequest is null)
            return OperationResult<ReportExportDownloadDto>.NotFoundResult($"Report export request {id} was not found.");

        if (exportRequest.Status != ReportExportRequestStatuses.Completed)
            return OperationResult<ReportExportDownloadDto>.Invalid($"Report export request {id} is {exportRequest.Status}.");

        if (string.IsNullOrWhiteSpace(exportRequest.BlobName) || string.IsNullOrWhiteSpace(exportRequest.FileName) || string.IsNullOrWhiteSpace(exportRequest.ContentType))
            return OperationResult<ReportExportDownloadDto>.NotFoundResult($"Report export request {id} does not have a generated file.");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var downloadUrl = await _blobStorageService.GetSasUrlAsync(_containerName, exportRequest.BlobName, expiresAt - DateTimeOffset.UtcNow);

        return OperationResult<ReportExportDownloadDto>.Success(new ReportExportDownloadDto
        {
            Id = exportRequest.Id,
            Status = exportRequest.Status,
            FileName = exportRequest.FileName,
            ContentType = exportRequest.ContentType,
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt
        });
    }

    public async Task ProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exportRequest = await _dbContext.ReportExportRequests
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (exportRequest is null)
        {
            _logger.LogWarning("Report export request {RequestId} was not found.", id);
            return;
        }

        if (exportRequest.Status == ReportExportRequestStatuses.Completed)
        {
            _logger.LogInformation("Report export request {RequestId} is already completed.", id);
            return;
        }

        try
        {
            exportRequest.Status = ReportExportRequestStatuses.Processing;
            exportRequest.ProcessingStartedAt ??= DateTimeOffset.UtcNow;
            exportRequest.FailedAt = null;
            exportRequest.ErrorMessage = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var fileNamePrefix = $"{exportRequest.ReportType}-report-{exportRequest.Id:N}";
            var reportFile = exportRequest.ReportType switch
            {
                "orders" => await _reportFileService.BuildOrdersReportAsync(exportRequest.Format, exportRequest.From, exportRequest.To, fileNamePrefix, cancellationToken),
                "inventory" => await _reportFileService.BuildInventoryReportAsync(exportRequest.Format, fileNamePrefix, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported report type '{exportRequest.ReportType}'.")
            };

            var blobName = ReportExportStorageConventions.GetBlobName(exportRequest.Id, exportRequest.ReportType, exportRequest.Format);
            await using var stream = new MemoryStream(reportFile.Content);
            var blobUrl = await _blobStorageService.UploadAsync(_containerName, blobName, stream, reportFile.ContentType);

            exportRequest.Status = ReportExportRequestStatuses.Completed;
            exportRequest.BlobName = blobName;
            exportRequest.BlobUrl = blobUrl;
            exportRequest.FileName = reportFile.FileName;
            exportRequest.ContentType = reportFile.ContentType;
            exportRequest.FileSizeBytes = reportFile.Content.LongLength;
            exportRequest.CompletedAt = DateTimeOffset.UtcNow;
            exportRequest.FailedAt = null;
            exportRequest.ErrorMessage = null;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            exportRequest.Status = ReportExportRequestStatuses.Failed;
            exportRequest.CompletedAt = null;
            exportRequest.FailedAt = DateTimeOffset.UtcNow;
            exportRequest.ErrorMessage = TruncateErrorMessage(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Report export request {RequestId} failed.", id);
        }
    }

    private static ReportExportRequestDto MapToDto(ReportExportRequest request) => new()
    {
        Id = request.Id,
        Status = request.Status,
        ReportType = request.ReportType,
        Format = request.Format,
        From = request.From,
        To = request.To,
        RequestedAt = request.RequestedAt,
        ProcessingStartedAt = request.ProcessingStartedAt,
        CompletedAt = request.CompletedAt,
        FailedAt = request.FailedAt,
        ErrorMessage = request.ErrorMessage,
        DownloadAvailable = request.Status == ReportExportRequestStatuses.Completed && !string.IsNullOrWhiteSpace(request.BlobName),
        FileName = request.FileName,
        ContentType = request.ContentType,
        FileSizeBytes = request.FileSizeBytes
    };

    private static string TruncateErrorMessage(string message)
        => message.Length <= 2000 ? message : message[..2000];
}
