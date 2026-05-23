namespace OutdoorsShop.Core.DTOs.Products;

public class ProductDto
{
    public int ProductID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int QuantityAvailable { get; set; }
}
