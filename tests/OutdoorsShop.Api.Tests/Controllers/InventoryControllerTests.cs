using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OutdoorsShop.Api.Controllers;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Tests.Controllers;

public class InventoryControllerTests
{
    private readonly Mock<IInventoryService> _inventoryService = new();

    private InventoryController CreateController(string role = "Administrator")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new InventoryController(_inventoryService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static InventoryDto MakeInventoryDto(int productId, int qty = 20, int threshold = 5) => new()
    {
        ProductID = productId,
        ProductName = $"Product {productId}",
        QuantityAvailable = qty,
        ReorderThreshold = threshold,
        LastUpdated = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_ReturnsPaged_ForAdmin()
    {
        var paged = new PagedResult<InventoryDto>
        {
            Items = [MakeInventoryDto(1), MakeInventoryDto(2)],
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 2
        };
        _inventoryService.Setup(s => s.GetPagedAsync(1, 20)).ReturnsAsync(paged);

        var result = await CreateController("Administrator").GetAll(1, 20);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResult<InventoryDto>>()
            .Which.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByProductId_Returns404_WhenNotFound()
    {
        _inventoryService
            .Setup(s => s.GetByProductIdAsync(99))
            .ReturnsAsync(OperationResult<InventoryDto>.NotFoundResult("Not found."));

        var result = await CreateController().GetByProductId(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByProductId_ReturnsInventory_WhenFound()
    {
        var dto = MakeInventoryDto(5);
        _inventoryService
            .Setup(s => s.GetByProductIdAsync(5))
            .ReturnsAsync(OperationResult<InventoryDto>.Success(dto));

        var result = await CreateController().GetByProductId(5);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<InventoryDto>()
            .Which.ProductID.Should().Be(5);
    }

    [Fact]
    public async Task Update_UpdatesQuantity_ForAdmin()
    {
        var dto = MakeInventoryDto(3, qty: 50);
        var request = new UpdateInventoryDto { QuantityAvailable = 50 };
        _inventoryService
            .Setup(s => s.UpdateAsync(3, request))
            .ReturnsAsync(OperationResult<InventoryDto>.Success(dto));

        var result = await CreateController().Update(3, request);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<InventoryDto>()
            .Which.QuantityAvailable.Should().Be(50);
    }

    [Fact]
    public async Task Update_Returns404_WhenProductNotFound()
    {
        var request = new UpdateInventoryDto { QuantityAvailable = 10 };
        _inventoryService
            .Setup(s => s.UpdateAsync(99, request))
            .ReturnsAsync(OperationResult<InventoryDto>.NotFoundResult("Not found."));

        var result = await CreateController().Update(99, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetLowStock_ReturnsItemsBelowThreshold()
    {
        var lowStock = new List<InventoryDto>
        {
            MakeInventoryDto(1, qty: 2, threshold: 5),
            MakeInventoryDto(2, qty: 4, threshold: 5)
        };
        _inventoryService.Setup(s => s.GetLowStockAsync()).ReturnsAsync(lowStock);

        var result = await CreateController().GetLowStock();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<InventoryDto>>()
            .Which.Should().HaveCount(2)
            .And.OnlyContain(i => i.IsLowStock);
    }
}
