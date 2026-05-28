using System.Text.Json.Serialization;

namespace OutdoorsShop.Core.Messages;

public record StockUpdateMessage(
    [property: JsonPropertyName("productId")] int ProductId,
    [property: JsonPropertyName("quantityDelta")] int QuantityDelta,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt
);
