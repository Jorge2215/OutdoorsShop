using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.Entities;

public class SalesOrder
{
    public int OrderID { get; set; }

    public int CustomerID { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
}
