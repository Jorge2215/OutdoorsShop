namespace OutdoorsShop.Core.Entities;

public class ProductInventory
{
    public int ProductID { get; set; }

    public int QuantityAvailable { get; set; }

    public DateTime LastUpdated { get; set; }

    public int ReorderThreshold { get; set; }

    // Navigation property
    public Product Product { get; set; } = null!;
}
