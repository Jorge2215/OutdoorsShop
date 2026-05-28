namespace OutdoorsShop.Core.Messages;

public static class ReportExportStorageConventions
{
    public const string DefaultContainerName = "report-exports";
    public const string DefaultQueueName = "report-export-requests";

    public static string GetBlobName(Guid requestId, string reportType, string format)
        => $"{reportType}/{requestId:N}.{GetFileExtension(format)}";

    private static string GetFileExtension(string format)
        => format.ToLowerInvariant() switch
        {
            "csv" => "csv",
            "excel" => "xlsx",
            _ => "bin"
        };
}
