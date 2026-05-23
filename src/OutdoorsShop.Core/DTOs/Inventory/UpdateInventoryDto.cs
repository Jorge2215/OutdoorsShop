using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.DTOs.Inventory;

public class UpdateInventoryDto
{
    [Range(0, int.MaxValue, ErrorMessage = "QuantityAvailable must be zero or greater.")]
    public int? QuantityAvailable { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "ReorderThreshold must be zero or greater.")]
    public int? ReorderThreshold { get; set; }
}
