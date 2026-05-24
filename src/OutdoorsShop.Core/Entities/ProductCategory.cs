using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.Entities;

public class ProductCategory
{
    public int CategoryID { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
