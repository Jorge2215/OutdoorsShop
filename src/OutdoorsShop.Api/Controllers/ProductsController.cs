using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Products;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public ProductsController(IProductRepository productRepository, IInventoryRepository inventoryRepository)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Get all active products.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? search)
    {
        IEnumerable<Core.Entities.Product> products;

        if (!string.IsNullOrWhiteSpace(search))
            products = await _productRepository.SearchAsync(search);
        else if (categoryId.HasValue)
            products = await _productRepository.GetByCategoryAsync(categoryId.Value);
        else
            products = await _productRepository.GetAllAsync();

        var dtos = products.Select(p => new ProductDto
        {
            ProductID = p.ProductID,
            Name = p.Name,
            CategoryID = p.CategoryID,
            CategoryName = p.Category?.Name ?? string.Empty,
            Price = p.Price,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive
        });

        return Ok(dtos);
    }

    /// <summary>Get a product by ID.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        var inventory = await _inventoryRepository.GetByProductIdAsync(id);

        var dto = new ProductDto
        {
            ProductID = product.ProductID,
            Name = product.Name,
            CategoryID = product.CategoryID,
            CategoryName = product.Category?.Name ?? string.Empty,
            Price = product.Price,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            QuantityAvailable = inventory?.QuantityAvailable ?? 0
        };

        return Ok(dto);
    }

    /// <summary>Create a new product (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = new Core.Entities.Product
        {
            Name = dto.Name,
            CategoryID = dto.CategoryID,
            Price = dto.Price,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IsActive = true
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.ProductID }, new ProductDto
        {
            ProductID = product.ProductID,
            Name = product.Name,
            CategoryID = product.CategoryID,
            Price = product.Price,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive
        });
    }

    /// <summary>Update a product (Admin only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        product.Name = dto.Name;
        product.CategoryID = dto.CategoryID;
        product.Price = dto.Price;
        product.Description = dto.Description;
        product.ImageUrl = dto.ImageUrl;
        product.IsActive = dto.IsActive;

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Soft-delete a product (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        product.IsActive = false;
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        return NoContent();
    }
}
