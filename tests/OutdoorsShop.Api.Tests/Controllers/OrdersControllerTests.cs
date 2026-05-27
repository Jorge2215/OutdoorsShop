using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OutdoorsShop.Api.Controllers;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _orderService = new();

    private OrdersController CreateController(string role, int customerId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimTypes.Role, role),
            new("customer_id", customerId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new OrdersController(_orderService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static OrderDto MakeOrderDto(int id, int customerId) => new()
    {
        OrderID = id,
        CustomerID = customerId,
        OrderDate = DateTime.UtcNow,
        ShippingAddress = "123 Main St",
        PaymentMethod = "CreditCard",
        TotalAmount = 199.99m,
        Status = OrderStatus.Pending,
        PaymentStatus = PaymentStatus.Pending
    };

    [Fact]
    public async Task Create_ReturnsCreated_WhenStockAvailable()
    {
        var request = new CreateOrderRequest
        {
            ShippingAddress = "123 Main St",
            PaymentMethod = "CreditCard",
            Items = [new OrderItemRequest { ProductID = 1, Quantity = 2, UnitPrice = 49.99m }]
        };
        var orderDto = MakeOrderDto(100, customerId: 5);
        _orderService
            .Setup(s => s.CreateAsync(5, request))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        var result = await CreateController("Customer", customerId: 5).Create(request);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_Returns400_WhenInsufficientStock()
    {
        var request = new CreateOrderRequest
        {
            ShippingAddress = "123 Main St",
            PaymentMethod = "CreditCard",
            Items = [new OrderItemRequest { ProductID = 1, Quantity = 9999, UnitPrice = 49.99m }]
        };
        _orderService
            .Setup(s => s.CreateAsync(5, request))
            .ReturnsAsync(OperationResult<OrderDto>.Invalid("Insufficient stock for Product 1."));

        var result = await CreateController("Customer", customerId: 5).Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Returns400_WhenCustomerIdClaimMissing()
    {
        // Controller without customer_id claim
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "Customer")
        ], "Test"));

        var controller = new OrdersController(_orderService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        var request = new CreateOrderRequest
        {
            ShippingAddress = "123 Main St",
            PaymentMethod = "CreditCard",
            Items = [new OrderItemRequest { ProductID = 1, Quantity = 1, UnitPrice = 9.99m }]
        };

        var result = await controller.Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOwnOrders_ForCustomerRole()
    {
        var pagedResult = new PagedResult<OrderDto>
        {
            Items = [MakeOrderDto(1, 5), MakeOrderDto(2, 5)],
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 2
        };
        _orderService
            .Setup(s => s.GetPagedAsync(1, 20, null, false, 5))
            .ReturnsAsync(pagedResult);

        var result = await CreateController("Customer", customerId: 5).GetAll(1, 20, null);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResult<OrderDto>>()
            .Which.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ReturnsAllOrders_ForAdminRole()
    {
        var pagedResult = new PagedResult<OrderDto>
        {
            Items = [MakeOrderDto(1, 5), MakeOrderDto(2, 6), MakeOrderDto(3, 7)],
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 3
        };
        _orderService
            .Setup(s => s.GetPagedAsync(1, 20, null, true, 1))
            .ReturnsAsync(pagedResult);

        var result = await CreateController("Administrator", customerId: 1).GetAll(1, 20, null);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResult<OrderDto>>()
            .Which.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetById_Returns403_WhenCustomerAccessesOtherOrder()
    {
        _orderService
            .Setup(s => s.GetByIdAsync(99, false, 5))
            .ReturnsAsync(OperationResult<OrderDto>.ForbiddenResult());

        var result = await CreateController("Customer", customerId: 5).GetById(99);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetReceipt_ReturnsOk_WhenReceiptIsAvailable()
    {
        _orderService
            .Setup(s => s.GetReceiptAsync(10, false, 5))
            .ReturnsAsync(OperationResult<OrderReceiptDto>.Success(new OrderReceiptDto
            {
                OrderID = 10,
                ReceiptAvailable = true,
                DownloadUrl = "https://storage.example/order-10"
            }));

        var result = await CreateController("Customer", customerId: 5).GetReceipt(10);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<OrderReceiptDto>()
            .Which.DownloadUrl.Should().Be("https://storage.example/order-10");
    }

    [Fact]
    public async Task GetReceipt_Returns403_WhenCustomerAccessesOtherOrderReceipt()
    {
        _orderService
            .Setup(s => s.GetReceiptAsync(99, false, 5))
            .ReturnsAsync(OperationResult<OrderReceiptDto>.ForbiddenResult());

        var result = await CreateController("Customer", customerId: 5).GetReceipt(99);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateStatus_Returns404_WhenOrderNotFound()
    {
        var request = new UpdateOrderStatusDto { Status = OrderStatus.Shipped };
        _orderService
            .Setup(s => s.UpdateStatusAsync(999, request))
            .ReturnsAsync(OperationResult<OrderDto>.NotFoundResult("Order not found."));

        var result = await CreateController("Administrator", customerId: 1).UpdateStatus(999, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_UpdatesStatus_ForAdminRole()
    {
        var orderDto = MakeOrderDto(10, 5);
        orderDto.Status = OrderStatus.Shipped;
        var request = new UpdateOrderStatusDto { Status = OrderStatus.Shipped };
        _orderService
            .Setup(s => s.UpdateStatusAsync(10, request))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        var result = await CreateController("Administrator", customerId: 1).UpdateStatus(10, request);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<OrderDto>()
            .Which.Status.Should().Be(OrderStatus.Shipped);
    }
}
