using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.DTOs.Orders;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public IList<CreateOrderItemDto> Items { get; set; } = new List<CreateOrderItemDto>();
}

public class CreateOrderItemDto
{
    [Required]
    public int ProductID { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
