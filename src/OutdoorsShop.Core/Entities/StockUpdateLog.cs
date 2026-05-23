namespace OutdoorsShop.Core.Entities;

public class StockUpdateLog
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public int ResultingQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
