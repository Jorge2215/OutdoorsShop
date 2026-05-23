using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.Entities;

public class Customer
{
    public int CustomerID { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string? Address { get; set; }

    // Navigation properties
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}
