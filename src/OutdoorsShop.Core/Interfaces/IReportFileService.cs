using OutdoorsShop.Core.DTOs.Reports;

namespace OutdoorsShop.Core.Interfaces;

public interface IReportFileService
{
    Task<GeneratedReportFileDto> BuildOrdersReportAsync(string format, DateTime? from, DateTime? to, string fileNamePrefix, CancellationToken cancellationToken = default);
    Task<GeneratedReportFileDto> BuildInventoryReportAsync(string format, string fileNamePrefix, CancellationToken cancellationToken = default);
}
