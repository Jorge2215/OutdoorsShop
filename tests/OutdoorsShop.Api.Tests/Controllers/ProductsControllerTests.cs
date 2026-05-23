using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OutdoorsShop.Api.Controllers;
using OutdoorsShop.Core.DTOs.Products;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryRepository> _inventoryRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();

    private ProductsController CreateController(string role = "Administrator")
    {
        var controller = new ProductsController(_productRepo.Object, _inventoryRepo.Object, _categoryRepo.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildUser(role)
            }
        };
        return controller;
    }

    private static ClaimsPrincipal BuildUser(string role, int customerId = 1) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, role),
            new Claim("customer_id", customerId.ToString())
        ], "Test"));

    private static Product MakeProduct(int id = 1, int categoryId = 1) => new()
    {
        ProductID = id,
        Name = $"Product {id}",
        CategoryID = categoryId,
        Price = 49.99m,
        IsActive = true,
        DiscountMultiplier = 1.0m
    };

    private static ProductInventory MakeInventory(int productId, int qty = 10) => new()
    {
        ProductID = productId,
        QuantityAvailable = qty,
        ReorderThreshold = 5,
        LastUpdated = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts_WhenNoFiltersApplied()
    {
        var products = new List<Product> { MakeProduct(1), MakeProduct(2) };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
        _inventoryRepo.Setup(r => r.GetByProductIdAsync(It.IsAny<int>())).ReturnsAsync((ProductInventory?)null);

        var controller = CreateController();
        var result = await controller.GetAll(null, null);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>()
            .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_FiltersByCategory_WhenCategoryIdProvided()
    {
        var products = new List<Product> { MakeProduct(1, categoryId: 2) };
        _productRepo.Setup(r => r.GetByCategoryAsync(2)).ReturnsAsync(products);
        _inventoryRepo.Setup(r => r.GetByProductIdAsync(It.IsAny<int>())).ReturnsAsync((ProductInventory?)null);

        var controller = CreateController();
        var result = await controller.GetAll(categoryId: 2, search: null);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>()
            .Which.Should().HaveCount(1);

        _productRepo.Verify(r => r.GetByCategoryAsync(2), Times.Once);
        _productRepo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetById_ReturnsProduct_WhenFound()
    {
        var product = MakeProduct(5);
        _productRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(product);
        _inventoryRepo.Setup(r => r.GetByProductIdAsync(5)).ReturnsAsync(MakeInventory(5, 7));

        var controller = CreateController();
        var result = await controller.GetById(5);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ProductDto>().Subject;
        dto.ProductID.Should().Be(5);
        dto.QuantityAvailable.Should().Be(7);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var controller = CreateController();
        var result = await controller.GetById(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenCategoryExists()
    {
        var category = new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true };
        var dto = new CreateProductDto { Name = "Tent", CategoryID = 1, Price = 99.99m };
        var createdProduct = new Product { ProductID = 10, Name = "Tent", CategoryID = 1, Price = 99.99m, IsActive = true };

        _categoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _productRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _inventoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductInventory>())).Returns(Task.CompletedTask);
        _inventoryRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(createdProduct);

        var controller = CreateController("Administrator");
        var result = await controller.Create(dto);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_Returns404_WhenCategoryNotFound()
    {
        var dto = new CreateProductDto { Name = "Tent", CategoryID = 999, Price = 99.99m };
        _categoryRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductCategory?)null);

        var controller = CreateController("Administrator");
        var result = await controller.Create(dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenProductExistsAndCategoryUnchanged()
    {
        var existing = MakeProduct(3, categoryId: 1);
        var dto = new UpdateProductDto { Name = "Updated Tent", CategoryID = 1, Price = 109.99m, IsActive = true };
        _productRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _productRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _inventoryRepo.Setup(r => r.GetByProductIdAsync(3)).ReturnsAsync(MakeInventory(3));

        var controller = CreateController("Administrator");
        var result = await controller.Update(3, dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenProductNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);
        var dto = new UpdateProductDto { Name = "X", CategoryID = 1, Price = 10m, IsActive = true };

        var controller = CreateController("Administrator");
        var result = await controller.Update(99, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_SetsIsActiveFalse_WhenProductExists()
    {
        var product = MakeProduct(7);
        _productRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(product);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _productRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var controller = CreateController("Administrator");
        var result = await controller.Delete(7);

        result.Should().BeOfType<NoContentResult>();
        product.IsActive.Should().BeFalse();
        _productRepo.Verify(r => r.UpdateAsync(It.Is<Product>(p => !p.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Delete_Returns404_WhenProductNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var controller = CreateController("Administrator");
        var result = await controller.Delete(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
