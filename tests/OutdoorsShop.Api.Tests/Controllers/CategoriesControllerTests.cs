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

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();

    private CategoriesController CreateController() =>
        new(_categoryRepo.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "admin-user"),
                        new Claim(ClaimTypes.Role, "Administrator")
                    ], "Test"))
                }
            }
        };

    [Fact]
    public async Task GetAll_ReturnsAllCategories()
    {
        var categories = new List<ProductCategory>
        {
            new() { CategoryID = 1, Name = "Camping", IsActive = true },
            new() { CategoryID = 2, Name = "Trekking", IsActive = true }
        };
        _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await CreateController().GetAll();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<CategoryDto>>()
            .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsCategory_WhenFound()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });

        var result = await CreateController().GetById(1);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<CategoryDto>()
            .Which.CategoryID.Should().Be(1);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ProductCategory?)null);

        var result = await CreateController().GetById(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreated_ForAdmin()
    {
        var dto = new CreateCategoryDto { Name = "Climbing" };
        _categoryRepo.Setup(r => r.AddAsync(It.IsAny<ProductCategory>())).Returns(Task.CompletedTask);
        _categoryRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await CreateController().Create(dto);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        var value = created.Value.Should().BeOfType<CategoryDto>().Subject;
        value.Name.Should().Be("Climbing");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenCategoryExists()
    {
        var existing = new ProductCategory { CategoryID = 2, Name = "Trekking", IsActive = true };
        var dto = new UpdateCategoryDto { Name = "Trekking Pro", IsActive = true };
        _categoryRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.UpdateAsync(It.IsAny<ProductCategory>())).Returns(Task.CompletedTask);
        _categoryRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await CreateController().Update(2, dto);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<CategoryDto>()
            .Which.Name.Should().Be("Trekking Pro");
    }

    [Fact]
    public async Task Delete_SoftDeletes_WhenCategoryExists()
    {
        var existing = new ProductCategory { CategoryID = 3, Name = "Cycling", IsActive = true };
        _categoryRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
        _categoryRepo.Setup(r => r.UpdateAsync(It.IsAny<ProductCategory>())).Returns(Task.CompletedTask);
        _categoryRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await CreateController().Delete(3);

        result.Should().BeOfType<NoContentResult>();
        existing.IsActive.Should().BeFalse();
        _categoryRepo.Verify(r => r.UpdateAsync(It.Is<ProductCategory>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Delete_Returns404_WhenCategoryNotFound()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ProductCategory?)null);

        var result = await CreateController().Delete(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
