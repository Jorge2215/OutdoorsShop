using System.Text.Json.Serialization;

namespace OutdoorsShop.Core.Messages;

public record ReportExportRequestMessage(
    [property: JsonPropertyName("requestId")] Guid RequestId
);
