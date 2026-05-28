using OutdoorsShop.Core.Messages;

namespace OutdoorsShop.Core.Interfaces;

public interface IStockUpdateQueuePublisher
{
    Task EnqueueAsync(StockUpdateMessage message, CancellationToken cancellationToken = default);
}
