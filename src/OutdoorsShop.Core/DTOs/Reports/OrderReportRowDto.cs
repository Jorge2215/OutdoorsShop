using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.DTOs.Reports;

public class OrderReportRowDto
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
}
