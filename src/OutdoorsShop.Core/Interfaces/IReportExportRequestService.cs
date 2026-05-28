using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Reports;

namespace OutdoorsShop.Core.Interfaces;

public interface IReportExportRequestService
{
    Task<OperationResult<ReportExportRequestDto>> CreateAsync(ReportExportRequestCreateDto request, string? requestedByUserId, CancellationToken cancellationToken = default);
    Task<OperationResult<ReportExportRequestDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult<ReportExportDownloadDto>> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);
    Task ProcessAsync(Guid id, CancellationToken cancellationToken = default);
}
