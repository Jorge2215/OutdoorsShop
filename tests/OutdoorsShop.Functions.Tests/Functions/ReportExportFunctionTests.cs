using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Functions.Functions;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Services;
using System.Text.Json;

namespace OutdoorsShop.Functions.Tests.Functions;

public class ReportExportFunctionTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureStorage:ReportExportsContainer"] = "report-exports"
        })
        .Build();

    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static string Serialize(ReportExportRequestMessage message) => JsonSerializer.Serialize(message);

    [Fact]
    public async Task Run_GeneratesOrdersCsvAndMarksRequestCompleted()
    {
        await using var db = CreateDbContext("report-export-success");
        await SeedOrdersReportRequestAsync(db, "csv");

        var blobStorage = new Mock<IBlobStorageService>();
        blobStorage
            .Setup(storage => storage.UploadAsync("report-exports", It.IsAny<string>(), It.IsAny<Stream>(), "text/csv"))
            .ReturnsAsync("https://storage.example/report-exports/orders/test.csv");

        var service = new ReportExportRequestService(
            db,
            new ReportFileService(db),
            blobStorage.Object,
            Mock.Of<IReportExportQueuePublisher>(),
            Configuration,
            NullLogger<ReportExportRequestService>.Instance);

        var function = new ReportExportFunction(service, NullLogger<ReportExportFunction>.Instance);
        var requestId = await db.ReportExportRequests.Select(r => r.Id).SingleAsync();

        await function.Run(Serialize(new ReportExportRequestMessage(requestId)));

        var stored = await db.ReportExportRequests.SingleAsync(r => r.Id == requestId);
        stored.Status.Should().Be(ReportExportRequestStatuses.Completed);
        stored.BlobName.Should().EndWith(".csv");
        stored.FileName.Should().EndWith(".csv");
        stored.ContentType.Should().Be("text/csv");
        stored.FileSizeBytes.Should().BeGreaterThan(0);
        blobStorage.Verify(storage => storage.UploadAsync("report-exports", It.IsAny<string>(), It.IsAny<Stream>(), "text/csv"), Times.Once);
    }

    [Fact]
    public async Task Run_MarksRequestFailed_WhenReportTypeIsUnsupported()
    {
        await using var db = CreateDbContext("report-export-failure");
        db.ReportExportRequests.Add(new ReportExportRequest
        {
            Id = Guid.NewGuid(),
            Status = ReportExportRequestStatuses.Pending,
            ReportType = "unknown",
            Format = "csv",
            RequestedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExportRequestService(
            db,
            new ReportFileService(db),
            Mock.Of<IBlobStorageService>(),
            Mock.Of<IReportExportQueuePublisher>(),
            Configuration,
            NullLogger<ReportExportRequestService>.Instance);

        var function = new ReportExportFunction(service, NullLogger<ReportExportFunction>.Instance);
        var requestId = await db.ReportExportRequests.Select(r => r.Id).SingleAsync();

        await function.Run(Serialize(new ReportExportRequestMessage(requestId)));

        var stored = await db.ReportExportRequests.SingleAsync(r => r.Id == requestId);
        stored.Status.Should().Be(ReportExportRequestStatuses.Failed);
        stored.ErrorMessage.Should().Contain("Unsupported report type");
    }

    private static async Task SeedOrdersReportRequestAsync(AppDbContext db, string format)
    {
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Customers.Add(new Customer
        {
            CustomerID = 10,
            UserId = "report-user-10",
            Name = "Report Customer",
            Email = "reports@test.com",
            IsActive = true
        });
        db.Products.Add(new Product
        {
            ProductID = 50,
            Name = "Lantern",
            CategoryID = 1,
            Price = 35m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        db.Orders.Add(new SalesOrder
        {
            OrderID = 901,
            CustomerID = 10,
            OrderDate = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc),
            ShippingAddress = "55 Export Trail",
            PaymentMethod = "CreditCard",
            TotalAmount = 70m,
            Status = Core.Enums.OrderStatus.Processing,
            PaymentStatus = Core.Enums.PaymentStatus.Confirmed,
            Details =
            [
                new SalesOrderDetail
                {
                    ProductID = 50,
                    Quantity = 2,
                    UnitPrice = 35m
                }
            ]
        });
        db.ReportExportRequests.Add(new ReportExportRequest
        {
            Id = Guid.NewGuid(),
            Status = ReportExportRequestStatuses.Pending,
            ReportType = "orders",
            Format = format,
            From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc),
            RequestedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
