namespace OutdoorsShop.Core.DTOs.Reports;

public class ReportExportDownloadDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
