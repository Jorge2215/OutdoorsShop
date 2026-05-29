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
    private const string DefaultSort = "name_asc";
    private const string PriceAscendingSort = "price_asc";
    private const string PriceDescendingSort = "price_desc";

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

    /// <summary>
    /// Returns the product catalog filtered by the supplied query parameters.
    /// </summary>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="search">Optional text search applied to the product name and description.</param>
    /// <param name="minPrice">Optional inclusive minimum price filter.</param>
    /// <param name="maxPrice">Optional inclusive maximum price filter.</param>
    /// <param name="sort">Optional sort value: <c>name_asc</c> (default), <c>price_asc</c>, or <c>price_desc</c>. Invalid values fall back to <c>name_asc</c>.</param>
    /// <param name="includeInactive">When true, administrators can include inactive products in the response.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort,
        [FromQuery] bool includeInactive = false)
    {
        IEnumerable<Product> products;

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            products = [];
        else if (includeInactive)
        {
            var unauthorized = EnsureAdminCanIncludeInactive();
            if (unauthorized is not null)
                return unauthorized;

            products = await _productRepo.GetAllIncludingInactiveAsync();
        }
        else
            products = await _productRepo.SearchProductsAsync(search, categoryId, minPrice, maxPrice, sort);

        if (includeInactive)
        {
            products = ApplyProductFilters(products, search, categoryId, minPrice, maxPrice);
            products = ApplyProductSort(products, sort);
        }

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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, [FromQuery] bool includeInactive = false)
    {
        if (includeInactive)
        {
            var unauthorized = EnsureAdminCanIncludeInactive();
            if (unauthorized is not null)
                return unauthorized;
        }

        var product = includeInactive
            ? await _productRepo.GetByIdIncludingInactiveAsync(id)
            : await _productRepo.GetByIdAsync(id);
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

    private IActionResult? EnsureAdminCanIncludeInactive()
        => User.IsInRole("Administrator")
            ? null
            : StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Administrator role is required to include inactive products." });

    private static IEnumerable<Product> ApplyProductFilters(
        IEnumerable<Product> products,
        string? search,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var filteredProducts = products;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredProducts = filteredProducts.Where(p => p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));
        }

        if (categoryId.HasValue)
            filteredProducts = filteredProducts.Where(p => p.CategoryID == categoryId.Value);

        if (minPrice.HasValue)
            filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);

        return filteredProducts;
    }

    private static IEnumerable<Product> ApplyProductSort(IEnumerable<Product> products, string? sort)
        => NormalizeSort(sort) switch
        {
            PriceAscendingSort => products.OrderBy(p => p.Price).ThenBy(p => p.Name),
            PriceDescendingSort => products.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
            _ => products.OrderBy(p => p.Name)
        };

    private static string NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return DefaultSort;

        return sort.Trim().ToLowerInvariant() switch
        {
            PriceAscendingSort => PriceAscendingSort,
            PriceDescendingSort => PriceDescendingSort,
            DefaultSort => DefaultSort,
            _ => DefaultSort
        };
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
