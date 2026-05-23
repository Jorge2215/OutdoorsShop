using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public OrdersController(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Get orders. Customers see their own; Admins see all.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        IEnumerable<SalesOrder> orders;

        if (User.IsInRole("Administrator"))
        {
            orders = await _orderRepository.GetAllAsync();
        }
        else
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var customer = await _customerRepository.GetByUserIdAsync(userId);
            if (customer is null)
                return Ok(Enumerable.Empty<OrderDto>());

            orders = await _orderRepository.GetByCustomerIdAsync(customer.CustomerID);
        }

        var dtos = orders.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>Get order by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order is null)
            return NotFound();

        if (!User.IsInRole("Administrator"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var customer = await _customerRepository.GetByUserIdAsync(userId);
            if (customer is null || order.CustomerID != customer.CustomerID)
                return Forbid();
        }

        return Ok(MapToDto(order));
    }

    /// <summary>Place a new order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var customer = await _customerRepository.GetByUserIdAsync(userId);
        if (customer is null)
            return BadRequest("Customer profile not found.");

        var order = new SalesOrder
        {
            CustomerID = customer.CustomerID,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };

        decimal total = 0;
        var details = new List<SalesOrderDetail>();

        foreach (var item in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductID);
            if (product is null)
                return BadRequest($"Product {item.ProductID} not found.");

            var detail = new SalesOrderDetail
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
            details.Add(detail);
            total += detail.UnitPrice * detail.Quantity;
        }

        order.TotalAmount = total;
        order.Details = details;

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.OrderID }, MapToDto(order));
    }

    /// <summary>Cancel an order (Customer: own orders; Admin: any).</summary>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return NotFound();

        if (!User.IsInRole("Administrator"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var customer = await _customerRepository.GetByUserIdAsync(userId);
            if (customer is null || order.CustomerID != customer.CustomerID)
                return Forbid();
        }

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            return BadRequest("Cannot cancel an order that has already been shipped or delivered.");

        order.Status = OrderStatus.Cancelled;
        await _orderRepository.UpdateAsync(order);
        await _orderRepository.SaveChangesAsync();

        return NoContent();
    }

    private static OrderDto MapToDto(SalesOrder order) => new()
    {
        OrderID = order.OrderID,
        CustomerID = order.CustomerID,
        OrderDate = order.OrderDate,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        PaymentStatus = order.PaymentStatus,
        Details = order.Details.Select(d => new OrderItemDto
        {
            OrderDetailID = d.OrderDetailID,
            ProductID = d.ProductID,
            ProductName = d.Product?.Name ?? string.Empty,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice
        }).ToList()
    };
}
