namespace OutdoorsShop.Core.DTOs.Inventory;

public class InventoryDto
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public DateTime LastUpdated { get; set; }
    public int ReorderThreshold { get; set; }
    public bool IsLowStock => QuantityAvailable <= ReorderThreshold;
}
