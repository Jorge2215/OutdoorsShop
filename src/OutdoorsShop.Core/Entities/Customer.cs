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

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? AvatarPath { get; set; }

    public string? AvatarContentType { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}
