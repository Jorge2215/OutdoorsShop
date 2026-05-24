using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.DTOs.Orders;

public class OrderDto
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public DateTime OrderDate { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public IList<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}
