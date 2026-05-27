using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

public class ReceiptGenerationFunctionTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureStorage:ReceiptsContainer"] = "order-receipts"
        })
        .Build();

    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static string Serialize(ReceiptGenerationMessage message) => JsonSerializer.Serialize(message);

    [Fact]
    public async Task Run_UploadsReceiptHtml_ForConfirmedOrder()
    {
        await using var db = CreateDbContext("receipt-generation-success");
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product
        {
            ProductID = 10,
            Name = "Tent",
            CategoryID = 1,
            Price = 100m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        db.Customers.Add(new Customer
        {
            CustomerID = 5,
            UserId = "user-5",
            Name = "Receipt Customer",
            Email = "receipt@test.com",
            IsActive = true
        });
        db.Orders.Add(new SalesOrder
        {
            OrderID = 501,
            CustomerID = 5,
            OrderDate = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc),
            ShippingAddress = "321 Ridge Trail",
            PaymentMethod = "CreditCard",
            TotalAmount = 200m,
            Status = OrderStatus.Processing,
            PaymentStatus = PaymentStatus.Confirmed,
            PaymentReference = "PAY-501",
            Details =
            [
                new SalesOrderDetail
                {
                    ProductID = 10,
                    Quantity = 2,
                    UnitPrice = 100m
                }
            ]
        });
        await db.SaveChangesAsync();

        var blobStorage = new Mock<IBlobStorageService>();
        string? uploadedHtml = null;
        blobStorage
            .Setup(storage => storage.UploadAsync("order-receipts", "orders/501/receipt.html", It.IsAny<Stream>(), "text/html; charset=utf-8"))
            .Callback<string, string, Stream, string>((_, _, stream, _) =>
            {
                using var reader = new StreamReader(stream);
                uploadedHtml = reader.ReadToEnd();
            })
            .ReturnsAsync("https://storage.example/orders/501/receipt.html");

        var function = new ReceiptGenerationFunction(
            db,
            blobStorage.Object,
            Configuration,
            NullLogger<ReceiptGenerationFunction>.Instance);

        var message = new ReceiptGenerationMessage(
            OrderId: 501,
            PaymentReference: "PAY-501",
            ConfirmedAt: new DateTimeOffset(2026, 5, 27, 12, 5, 0, TimeSpan.Zero),
            ReceiptBlobName: "orders/501/receipt.html");

        await function.Run(Serialize(message));

        blobStorage.Verify(storage => storage.DeleteAsync("order-receipts", "orders/501/receipt.html"), Times.Once);
        blobStorage.Verify(storage => storage.UploadAsync("order-receipts", "orders/501/receipt.html", It.IsAny<Stream>(), "text/html; charset=utf-8"), Times.Once);
        uploadedHtml.Should().NotBeNull();
        uploadedHtml.Should().Contain("Order Receipt #501");
        uploadedHtml.Should().Contain("Receipt Customer");
        uploadedHtml.Should().Contain("PAY-501");
        uploadedHtml.Should().Contain("Tent");
    }

    [Fact]
    public async Task Run_EncodesHtmlSensitiveFields_InGeneratedReceipt()
    {
        await using var db = CreateDbContext("receipt-generation-encoding");
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product
        {
            ProductID = 11,
            Name = "<script>alert('x')</script> Tent & Stove",
            CategoryID = 1,
            Price = 100m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        db.Customers.Add(new Customer
        {
            CustomerID = 6,
            UserId = "user-6",
            Name = "<b>Receipt Customer</b>",
            Email = "\"attacker\"@test.com",
            IsActive = true
        });
        db.Orders.Add(new SalesOrder
        {
            OrderID = 503,
            CustomerID = 6,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = "<img src=x onerror=alert('xss')>",
            PaymentMethod = "CreditCard",
            TotalAmount = 100m,
            Status = OrderStatus.Processing,
            PaymentStatus = PaymentStatus.Confirmed,
            PaymentReference = "<iframe>bad</iframe>",
            Details =
            [
                new SalesOrderDetail
                {
                    ProductID = 11,
                    Quantity = 1,
                    UnitPrice = 100m
                }
            ]
        });
        await db.SaveChangesAsync();

        var blobStorage = new Mock<IBlobStorageService>();
        string? uploadedHtml = null;
        blobStorage
            .Setup(storage => storage.UploadAsync("order-receipts", "orders/503/receipt.html", It.IsAny<Stream>(), "text/html; charset=utf-8"))
            .Callback<string, string, Stream, string>((_, _, stream, _) =>
            {
                using var reader = new StreamReader(stream);
                uploadedHtml = reader.ReadToEnd();
            })
            .ReturnsAsync("https://storage.example/orders/503/receipt.html");

        var function = new ReceiptGenerationFunction(
            db,
            blobStorage.Object,
            Configuration,
            NullLogger<ReceiptGenerationFunction>.Instance);

        var message = new ReceiptGenerationMessage(
            OrderId: 503,
            PaymentReference: "<iframe>bad</iframe>",
            ConfirmedAt: DateTimeOffset.UtcNow,
            ReceiptBlobName: "orders/503/receipt.html");

        await function.Run(Serialize(message));

        uploadedHtml.Should().NotBeNull();
        uploadedHtml.Should().Contain("&lt;b&gt;Receipt Customer&lt;/b&gt;");
        uploadedHtml.Should().Contain("&quot;attacker&quot;@test.com");
        uploadedHtml.Should().Contain("&lt;img src=x onerror=alert(&#39;xss&#39;)&gt;");
        uploadedHtml.Should().Contain("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; Tent &amp; Stove");
        uploadedHtml.Should().Contain("&lt;iframe&gt;bad&lt;/iframe&gt;");
        uploadedHtml.Should().NotContain("<script>alert('x')</script>");
        uploadedHtml.Should().NotContain("<img src=x onerror=alert('xss')>");
    }

    [Fact]
    public async Task Run_SkipsUpload_WhenOrderIsNotConfirmed()
    {
        await using var db = CreateDbContext("receipt-generation-unconfirmed");
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product
        {
            ProductID = 10,
            Name = "Tent",
            CategoryID = 1,
            Price = 100m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        db.Customers.Add(new Customer
        {
            CustomerID = 5,
            UserId = "user-5",
            Name = "Receipt Customer",
            Email = "receipt@test.com",
            IsActive = true
        });
        db.Orders.Add(new SalesOrder
        {
            OrderID = 502,
            CustomerID = 5,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = "321 Ridge Trail",
            PaymentMethod = "CreditCard",
            TotalAmount = 100m,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            Details =
            [
                new SalesOrderDetail
                {
                    ProductID = 10,
                    Quantity = 1,
                    UnitPrice = 100m
                }
            ]
        });
        await db.SaveChangesAsync();

        var blobStorage = new Mock<IBlobStorageService>();
        var function = new ReceiptGenerationFunction(
            db,
            blobStorage.Object,
            Configuration,
            NullLogger<ReceiptGenerationFunction>.Instance);

        var message = new ReceiptGenerationMessage(
            OrderId: 502,
            PaymentReference: "PAY-502",
            ConfirmedAt: DateTimeOffset.UtcNow,
            ReceiptBlobName: "orders/502/receipt.html");

        await function.Run(Serialize(message));

        blobStorage.Verify(storage => storage.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        blobStorage.Verify(storage => storage.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }
}
