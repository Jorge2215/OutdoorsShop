namespace OutdoorsShop.Core.Messages;

public static class OrderReceiptStorageConventions
{
    public const string DefaultContainerName = "order-receipts";
    public const string DefaultQueueName = "receipt-requests";

    public static string GetBlobName(int orderId)
        => $"orders/{orderId}/receipt.html";
}
