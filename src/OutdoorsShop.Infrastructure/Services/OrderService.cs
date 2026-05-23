using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Services;

public class OrderService : IOrderService
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly AppDbContext _dbContext;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        AppDbContext dbContext)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _dbContext = dbContext;
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status, bool isAdministrator, int? currentCustomerId)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        if (!isAdministrator && !currentCustomerId.HasValue)
        {
            return new PagedResult<OrderDto>
            {
                Items = [],
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = 0
            };
        }

        var customerIdFilter = isAdministrator ? null : currentCustomerId;
        var (items, totalCount) = await _orderRepository.GetPagedAsync(normalizedPageNumber, normalizedPageSize, status, customerIdFilter);

        return new PagedResult<OrderDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult<OrderDto>> GetByIdAsync(int id, bool isAdministrator, int? currentCustomerId)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order is null)
            return OperationResult<OrderDto>.NotFoundResult($"Order {id} not found.");

        if (!isAdministrator && order.CustomerID != currentCustomerId)
            return OperationResult<OrderDto>.ForbiddenResult("You can only access your own orders.");

        return OperationResult<OrderDto>.Success(MapToDto(order));
    }

    public async Task<OperationResult<OrderDto>> CreateAsync(int currentCustomerId, CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
            return OperationResult<OrderDto>.Invalid("Order must contain at least one item.");

        var customer = await _customerRepository.GetByIdAsync(currentCustomerId);
        if (customer is null)
            return OperationResult<OrderDto>.NotFoundResult($"Customer {currentCustomerId} not found.");

        var validatedItems = new List<(OrderItemRequest Request, Product Product, ProductInventory Inventory)>();

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductID);
            if (product is null)
                return OperationResult<OrderDto>.Invalid($"Product {item.ProductID} not found or inactive.");

            var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductID);
            if (inventory is null)
                return OperationResult<OrderDto>.Invalid($"Inventory for product {item.ProductID} not found.");

            if (inventory.QuantityAvailable < item.Quantity)
                return OperationResult<OrderDto>.Invalid($"Insufficient stock for product {product.Name}.");

            if (decimal.Round(item.UnitPrice, 2) != decimal.Round(product.Price, 2))
                return OperationResult<OrderDto>.Invalid($"Unit price for product {item.ProductID} does not match the current catalog price.");

            validatedItems.Add((item, product, inventory));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var order = new SalesOrder
        {
            CustomerID = currentCustomerId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = request.ShippingAddress.Trim(),
            PaymentMethod = request.PaymentMethod.Trim(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            Details = new List<SalesOrderDetail>()
        };

        foreach (var item in validatedItems)
        {
            item.Inventory.QuantityAvailable -= item.Request.Quantity;
            item.Inventory.LastUpdated = DateTime.UtcNow;

            order.Details.Add(new SalesOrderDetail
            {
                ProductID = item.Product.ProductID,
                Product = item.Product,
                Quantity = item.Request.Quantity,
                UnitPrice = item.Request.UnitPrice
            });
        }

        order.TotalAmount = order.Details.Sum(detail => detail.Quantity * detail.UnitPrice);

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdOrder = await _orderRepository.GetWithDetailsAsync(order.OrderID);
        return OperationResult<OrderDto>.Success(MapToDto(createdOrder ?? order));
    }

    public async Task<OperationResult<OrderDto>> UpdateStatusAsync(int id, UpdateOrderStatusDto request)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order is null)
            return OperationResult<OrderDto>.NotFoundResult($"Order {id} not found.");

        if (!CanTransition(order.Status, request.Status))
            return OperationResult<OrderDto>.Invalid($"Invalid status transition from {order.Status} to {request.Status}.");

        order.Status = request.Status;
        await _orderRepository.UpdateAsync(order);
        await _orderRepository.SaveChangesAsync();

        return OperationResult<OrderDto>.Success(MapToDto(order));
    }

    public async Task<OperationResult> CancelAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return OperationResult.NotFoundResult($"Order {id} not found.");

        if (order.Status == OrderStatus.Cancelled)
            return OperationResult.Success();

        if (!CanTransition(order.Status, OrderStatus.Cancelled))
            return OperationResult.Invalid($"Order {id} can no longer be cancelled.");

        order.Status = OrderStatus.Cancelled;
        await _orderRepository.UpdateAsync(order);
        await _orderRepository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<OrderReportRowDto>> GetReportRowsAsync(DateTime? from, DateTime? to)
    {
        var orders = await _orderRepository.GetForReportAsync(from, to);
        return orders.Select(order => new OrderReportRowDto
        {
            OrderID = order.OrderID,
            CustomerID = order.CustomerID,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            ItemCount = order.Details.Sum(detail => detail.Quantity),
            ShippingAddress = order.ShippingAddress
        }).ToList();
    }

    private static bool CanTransition(OrderStatus currentStatus, OrderStatus nextStatus)
        => AllowedTransitions.TryGetValue(currentStatus, out var nextStatuses) && nextStatuses.Contains(nextStatus);

    private static OrderDto MapToDto(SalesOrder order) => new()
    {
        OrderID = order.OrderID,
        CustomerID = order.CustomerID,
        OrderDate = order.OrderDate,
        ShippingAddress = order.ShippingAddress,
        PaymentMethod = order.PaymentMethod,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        PaymentStatus = order.PaymentStatus,
        Items = order.Details.Select(detail => new OrderItemDto
        {
            OrderDetailID = detail.OrderDetailID,
            ProductID = detail.ProductID,
            ProductName = detail.Product?.Name ?? string.Empty,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice
        }).ToList()
    };
}
