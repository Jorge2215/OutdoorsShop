using System.ComponentModel.DataAnnotations;
using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.Entities;

public class SalesOrder
{
    public int OrderID { get; set; }

    public int CustomerID { get; set; }

    public DateTime OrderDate { get; set; }

    [Required]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public string? PaymentReference { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
}
