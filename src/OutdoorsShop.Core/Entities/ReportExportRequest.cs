namespace OutdoorsShop.Core.Entities;

public class ReportExportRequest
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? BlobName { get; set; }
    public string? BlobUrl { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestedByUserId { get; set; }
}
