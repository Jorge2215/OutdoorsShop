using System.Text.Json.Serialization;

namespace OutdoorsShop.Core.Messages;

public record ReceiptGenerationMessage(
    [property: JsonPropertyName("orderId")] int OrderId,
    [property: JsonPropertyName("paymentReference")] string PaymentReference,
    [property: JsonPropertyName("confirmedAt")] DateTimeOffset ConfirmedAt,
    [property: JsonPropertyName("receiptBlobName")] string ReceiptBlobName
);
