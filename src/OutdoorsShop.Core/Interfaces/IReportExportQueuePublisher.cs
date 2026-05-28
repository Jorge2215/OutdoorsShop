using OutdoorsShop.Core.Messages;

namespace OutdoorsShop.Core.Interfaces;

public interface IReportExportQueuePublisher
{
    Task EnqueueAsync(ReportExportRequestMessage message, CancellationToken cancellationToken = default);
}
