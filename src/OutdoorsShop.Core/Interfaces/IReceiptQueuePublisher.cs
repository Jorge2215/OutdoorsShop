using OutdoorsShop.Core.Messages;

namespace OutdoorsShop.Core.Interfaces;

public interface IReceiptQueuePublisher
{
    Task EnqueueAsync(ReceiptGenerationMessage message, CancellationToken cancellationToken = default);
}
