using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.Entities;

public class Product
{
    public int ProductID { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int CategoryID { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ProductCategory Category { get; set; } = null!;
    public ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();
}
