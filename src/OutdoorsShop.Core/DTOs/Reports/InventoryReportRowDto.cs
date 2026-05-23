namespace OutdoorsShop.Core.DTOs.Reports;

public class InventoryReportRowDto
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int ReorderThreshold { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsLowStock { get; set; }
}
