using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Identity;
using OutdoorsShop.Infrastructure.Repositories;
using OutdoorsShop.Infrastructure.Services;

namespace OutdoorsShop.Api.Tests.Services;

public class OrderServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AppDbContext _db = null!;
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureStorage:ReceiptsContainer"] = "order-receipts"
        })
        .Build();

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _db.Users.Add(new ApplicationUser
        {
            Id = "customer-42",
            UserName = "customer@test.com",
            NormalizedUserName = "CUSTOMER@TEST.COM",
            Email = "customer@test.com",
            NormalizedEmail = "CUSTOMER@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        _db.Products.Add(new Product
        {
            ProductID = 3,
            Name = "Backpack",
            CategoryID = 1,
            Price = 25m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        _db.Inventory.Add(new ProductInventory
        {
            ProductID = 3,
            QuantityAvailable = 12,
            ReorderThreshold = 2,
            LastUpdated = DateTime.UtcNow
        });
        _db.Customers.Add(new Customer
        {
            CustomerID = 42,
            UserId = "customer-42",
            Name = "Test Customer",
            Email = "customer@test.com",
            IsActive = true
        });
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_DeductsInventoryLogsAndPublishesAggregatedQueueMessage()
    {
        var queuePublisher = new Mock<IStockUpdateQueuePublisher>();
        var blobStorageService = new Mock<IBlobStorageService>();
        queuePublisher.Setup(q => q.EnqueueAsync(It.IsAny<StockUpdateMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new OrderService(
            new OrderRepository(_db),
            new CustomerRepository(_db),
            new ProductRepository(_db),
            new InventoryRepository(_db),
            _db,
            queuePublisher.Object,
            blobStorageService.Object,
            _configuration,
            NullLogger<OrderService>.Instance);

        var request = new CreateOrderRequest
        {
            ShippingAddress = "123 Trail Rd",
            PaymentMethod = "CreditCard",
            Items =
            [
                new OrderItemRequest { ProductID = 3, Quantity = 2, UnitPrice = 25m },
                new OrderItemRequest { ProductID = 3, Quantity = 1, UnitPrice = 25m }
            ]
        };

        var result = await service.CreateAsync(42, request);

        result.Succeeded.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);

        var inventory = await _db.Inventory.FindAsync(3);
        inventory!.QuantityAvailable.Should().Be(9);

        var log = await _db.StockUpdateLogs.SingleAsync();
        log.ProductId.Should().Be(3);
        log.QuantityDelta.Should().Be(-3);
        log.ResultingQuantity.Should().Be(9);
        log.Reason.Should().Be("OrderPlacement");
        log.Notes.Should().Be("Order stock deduction");

        queuePublisher.Verify(q => q.EnqueueAsync(
            It.Is<StockUpdateMessage>(message =>
                message.ProductId == 3 &&
                message.QuantityDelta == -3 &&
                message.Reason == "OrderPlacement" &&
                message.Notes == "Order stock deduction"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetReceiptAsync_ReturnsSasUrl_WhenReceiptExistsForConfirmedOrder()
    {
        _db.Orders.Add(new SalesOrder
        {
            OrderID = 700,
            CustomerID = 42,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = "123 Trail Rd",
            PaymentMethod = "CreditCard",
            TotalAmount = 25m,
            Status = OrderStatus.Processing,
            PaymentStatus = PaymentStatus.Confirmed,
            PaymentReference = "PAY-700"
        });
        await _db.SaveChangesAsync();

        var queuePublisher = new Mock<IStockUpdateQueuePublisher>();
        var blobStorageService = new Mock<IBlobStorageService>();
        blobStorageService
            .Setup(s => s.ExistsAsync("order-receipts", "orders/700/receipt.html"))
            .ReturnsAsync(true);
        blobStorageService
            .Setup(s => s.GetSasUrlAsync("order-receipts", "orders/700/receipt.html", It.IsAny<TimeSpan>()))
            .ReturnsAsync("https://storage.example/receipts/700");

        var service = new OrderService(
            new OrderRepository(_db),
            new CustomerRepository(_db),
            new ProductRepository(_db),
            new InventoryRepository(_db),
            _db,
            queuePublisher.Object,
            blobStorageService.Object,
            _configuration,
            NullLogger<OrderService>.Instance);

        var result = await service.GetReceiptAsync(700, isAdministrator: false, currentCustomerId: 42);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReceiptAvailable.Should().BeTrue();
        result.Value.DownloadUrl.Should().Be("https://storage.example/receipts/700");
    }
}
