using System.ComponentModel.DataAnnotations;
using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
