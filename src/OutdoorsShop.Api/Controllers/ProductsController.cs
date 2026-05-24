using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Products;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IBlobStorageService _blobStorage;

    public ProductsController(
        IProductRepository productRepo,
        IInventoryRepository inventoryRepo,
        ICategoryRepository categoryRepo,
        IBlobStorageService blobStorage)
    {
        _productRepo = productRepo;
        _inventoryRepo = inventoryRepo;
        _categoryRepo = categoryRepo;
        _blobStorage = blobStorage;
    }

    // GET /api/v1/products?categoryId=1&search=tent
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? search)
    {
        IEnumerable<Product> products;

        if (!string.IsNullOrWhiteSpace(search))
            products = await _productRepo.SearchAsync(search);
        else if (categoryId.HasValue)
            products = await _productRepo.GetByCategoryAsync(categoryId.Value);
        else
            products = await _productRepo.GetAllAsync();

        var productIds = products.Select(p => p.ProductID).ToList();
        var allInventory = new Dictionary<int, int>();
        foreach (var pid in productIds)
        {
            var inv = await _inventoryRepo.GetByProductIdAsync(pid);
            if (inv is not null)
                allInventory[pid] = inv.QuantityAvailable;
        }

        var dtos = products.Select(p => ToDto(p, allInventory.GetValueOrDefault(p.ProductID, 0)));
        return Ok(dtos);
    }

    // GET /api/v1/products/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        var inventory = await _inventoryRepo.GetByProductIdAsync(id);
        return Ok(ToDto(product, inventory?.QuantityAvailable ?? 0));
    }

    // POST /api/v1/products  [Administrator]
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var category = await _categoryRepo.GetByIdAsync(dto.CategoryID);
        if (category is null)
            return NotFound(new { message = $"Category {dto.CategoryID} not found." });

        var product = new Product
        {
            Name = dto.Name,
            CategoryID = dto.CategoryID,
            Price = dto.Price,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IsActive = true
        };

        await _productRepo.AddAsync(product);
        await _productRepo.SaveChangesAsync();

        var inventory = new ProductInventory
        {
            ProductID = product.ProductID,
            QuantityAvailable = 0,
            ReorderThreshold = 5,
            LastUpdated = DateTime.UtcNow
        };
        await _inventoryRepo.AddAsync(inventory);
        await _inventoryRepo.SaveChangesAsync();

        var created = await _productRepo.GetByIdAsync(product.ProductID);
        return CreatedAtAction(nameof(GetById), new { id = product.ProductID }, ToDto(created!, 0));
    }

    // PUT /api/v1/products/{id}  [Administrator]
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        if (dto.CategoryID != product.CategoryID)
        {
            var category = await _categoryRepo.GetByIdAsync(dto.CategoryID);
            if (category is null)
                return NotFound(new { message = $"Category {dto.CategoryID} not found." });
        }

        product.Name = dto.Name;
        product.CategoryID = dto.CategoryID;
        product.Price = dto.Price;
        product.Description = dto.Description;
        product.ImageUrl = dto.ImageUrl;
        product.IsActive = dto.IsActive;

        await _productRepo.UpdateAsync(product);
        await _productRepo.SaveChangesAsync();

        var inventory = await _inventoryRepo.GetByProductIdAsync(id);
        return Ok(ToDto(product, inventory?.QuantityAvailable ?? 0));
    }

    // DELETE /api/v1/products/{id}  [Administrator] — soft delete
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        product.IsActive = false;
        await _productRepo.UpdateAsync(product);
        await _productRepo.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/v1/products/{id}/image  [Administrator]
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Administrator")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        if (!allowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Invalid file type. Allowed types: jpg, jpeg, png, gif, webp." });

        const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds the 5 MB limit." });

        using var stream = file.OpenReadStream();
        var imageUrl = await _blobStorage.UploadProductImageAsync(stream, file.FileName, file.ContentType, id);

        product.ImageUrl = imageUrl;
        await _productRepo.UpdateAsync(product);
        await _productRepo.SaveChangesAsync();

        return Ok(new { imageUrl });
    }

    private static ProductDto ToDto(Product p, int quantityAvailable) => new()
    {
        ProductID = p.ProductID,
        Name = p.Name,
        CategoryID = p.CategoryID,
        CategoryName = p.Category?.Name ?? string.Empty,
        Price = p.Price,
        Description = p.Description,
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        QuantityAvailable = quantityAvailable
    };
}
