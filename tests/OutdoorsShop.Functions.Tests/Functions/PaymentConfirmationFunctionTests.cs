using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Functions.Functions;
using OutdoorsShop.Infrastructure.Data;
using System.Text.Json;

namespace OutdoorsShop.Functions.Tests.Functions;

public class PaymentConfirmationFunctionTests
{
    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static string Serialize(PaymentConfirmationMessage msg) =>
        JsonSerializer.Serialize(msg);

    private static async Task<SalesOrder> SeedOrderAsync(
        AppDbContext db,
        int orderId = 1,
        int customerId = 1,
        params (int productId, int qty)[] items)
    {
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });

        db.Customers.Add(new Customer
        {
            CustomerID = customerId,
            UserId = $"user-{customerId}",
            Name = "Test Customer",
            Email = "test@test.com",
            IsActive = true
        });

        var order = new SalesOrder
        {
            OrderID = orderId,
            CustomerID = customerId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = "123 Main St",
            PaymentMethod = "CreditCard",
            TotalAmount = 100m,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };

        foreach (var (productId, qty) in items)
        {
            db.Products.Add(new Product
            {
                ProductID = productId,
                Name = $"Product {productId}",
                CategoryID = 1,
                Price = 50m,
                IsActive = true,
                DiscountMultiplier = 1.0m
            });
            db.Inventory.Add(new ProductInventory
            {
                ProductID = productId,
                QuantityAvailable = qty,
                ReorderThreshold = 5,
                LastUpdated = DateTime.UtcNow
            });
            order.Details.Add(new SalesOrderDetail
            {
                ProductID = productId,
                Quantity = qty,
                UnitPrice = 50m
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task Run_SetsOrderToProcessing_OnSuccessPayment()
    {
        await using var db = CreateDbContext("payment-success-status");
        await SeedOrderAsync(db, orderId: 1, customerId: 1, (productId: 10, qty: 2));
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();
        receiptQueuePublisher
            .Setup(publisher => publisher.EnqueueAsync(It.IsAny<ReceiptGenerationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = new PaymentConfirmationMessage(
            OrderId: 1,
            PaymentReference: "PAY-001",
            PaymentStatus: "Success",
            Amount: 100m,
            ProcessedAt: DateTimeOffset.UtcNow);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);
        await function.Run(Serialize(message));

        var order = await db.Orders.FindAsync(1);
        order!.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task Run_StampsPaymentReference_OnSuccessPayment()
    {
        await using var db = CreateDbContext("payment-success-reference");
        await SeedOrderAsync(db, orderId: 2, customerId: 1, (productId: 11, qty: 1));
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();
        receiptQueuePublisher
            .Setup(publisher => publisher.EnqueueAsync(It.IsAny<ReceiptGenerationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var processedAt = DateTimeOffset.UtcNow;
        var message = new PaymentConfirmationMessage(
            OrderId: 2,
            PaymentReference: "PAY-XYZ-123",
            PaymentStatus: "Success",
            Amount: 50m,
            ProcessedAt: processedAt);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);
        await function.Run(Serialize(message));

        var order = await db.Orders.FindAsync(2);
        order!.PaymentReference.Should().Be("PAY-XYZ-123");
        order.PaymentStatus.Should().Be(PaymentStatus.Confirmed);
        order.PaidAt.Should().Be(processedAt);

        receiptQueuePublisher.Verify(
            publisher => publisher.EnqueueAsync(
                It.Is<ReceiptGenerationMessage>(queued =>
                    queued.OrderId == 2 &&
                    queued.PaymentReference == "PAY-XYZ-123" &&
                    queued.ReceiptBlobName == "orders/2/receipt.html"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_SetsOrderToCancelled_OnFailedPayment()
    {
        await using var db = CreateDbContext("payment-failed-status");
        await SeedOrderAsync(db, orderId: 3, customerId: 1, (productId: 12, qty: 3));
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();

        var message = new PaymentConfirmationMessage(
            OrderId: 3,
            PaymentReference: "",
            PaymentStatus: "Failed",
            Amount: 150m,
            ProcessedAt: DateTimeOffset.UtcNow);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);
        await function.Run(Serialize(message));

        var order = await db.Orders.FindAsync(3);
        order!.Status.Should().Be(OrderStatus.Cancelled);
        order.PaymentStatus.Should().Be(PaymentStatus.Failed);

        receiptQueuePublisher.Verify(
            publisher => publisher.EnqueueAsync(It.IsAny<ReceiptGenerationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_RestoresInventory_OnFailedPayment()
    {
        await using var db = CreateDbContext("payment-failed-inventory");
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();
        // seed: product 20 with 5 units remaining
        await SeedOrderAsync(db, orderId: 4, customerId: 1, (productId: 20, qty: 5));

        var message = new PaymentConfirmationMessage(
            OrderId: 4,
            PaymentReference: "",
            PaymentStatus: "Failed",
            Amount: 250m,
            ProcessedAt: DateTimeOffset.UtcNow);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);
        await function.Run(Serialize(message));

        // qty was 5, order had qty=5, restore should bring it to 10
        var inv = await db.Inventory.FindAsync(20);
        inv!.QuantityAvailable.Should().Be(10,
            "inventory for each order item must be restored when payment fails");
    }

    [Fact]
    public async Task Run_DoesNotChangeOrder_OnPendingPayment()
    {
        await using var db = CreateDbContext("payment-pending");
        await SeedOrderAsync(db, orderId: 5, customerId: 1, (productId: 30, qty: 1));
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();

        var message = new PaymentConfirmationMessage(
            OrderId: 5,
            PaymentReference: "",
            PaymentStatus: "Pending",
            Amount: 50m,
            ProcessedAt: DateTimeOffset.UtcNow);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);
        await function.Run(Serialize(message));

        var order = await db.Orders.FindAsync(5);
        order!.Status.Should().Be(OrderStatus.Pending,
            "pending payment must not change order status");
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Run_HandlesOrderNotFound_Gracefully()
    {
        await using var db = CreateDbContext("payment-order-not-found");
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();

        var message = new PaymentConfirmationMessage(
            OrderId: 9999,
            PaymentReference: "REF",
            PaymentStatus: "Success",
            Amount: 100m,
            ProcessedAt: DateTimeOffset.UtcNow);

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);

        // Should complete without throwing
        var act = async () => await function.Run(Serialize(message));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Run_HandlesInvalidJson_Gracefully()
    {
        await using var db = CreateDbContext("payment-invalid-json");
        var receiptQueuePublisher = new Mock<IReceiptQueuePublisher>();

        var function = new PaymentConfirmationFunction(db, receiptQueuePublisher.Object, NullLogger<PaymentConfirmationFunction>.Instance);

        var act = async () => await function.Run("{ this is not valid json !!!");
        await act.Should().NotThrowAsync();
    }
}
