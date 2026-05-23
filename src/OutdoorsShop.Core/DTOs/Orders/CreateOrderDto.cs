using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.DTOs.Orders;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public IList<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
}

public class OrderItemRequest
{
    [Required]
    public int ProductID { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "UnitPrice must be greater than zero.")]
    public decimal UnitPrice { get; set; }
}
