namespace OutdoorsShop.Core.Entities;

public class SalesOrderDetail
{
    public int OrderDetailID { get; set; }

    public int OrderID { get; set; }

    public int ProductID { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    // Navigation properties
    public SalesOrder Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
