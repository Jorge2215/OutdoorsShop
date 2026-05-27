using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Repositories;
using OutdoorsShop.Infrastructure.Services;

namespace OutdoorsShop.Api.Tests.Services;

public class InventoryServiceTests
{
    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesInventoryAndPublishesDelta_WhenQuantityChanges()
    {
        await using var db = CreateDbContext(nameof(UpdateAsync_UpdatesInventoryAndPublishesDelta_WhenQuantityChanges));
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product
        {
            ProductID = 7,
            Name = "Tent",
            CategoryID = 1,
            Price = 99m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        db.Inventory.Add(new ProductInventory
        {
            ProductID = 7,
            QuantityAvailable = 10,
            ReorderThreshold = 3,
            LastUpdated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var queuePublisher = new Mock<IStockUpdateQueuePublisher>();
        queuePublisher.Setup(q => q.EnqueueAsync(It.IsAny<StockUpdateMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new InventoryService(
            new InventoryRepository(db),
            db,
            queuePublisher.Object,
            NullLogger<InventoryService>.Instance);

        var result = await service.UpdateAsync(7, new UpdateInventoryDto { QuantityAvailable = 14 });

        result.Succeeded.Should().BeTrue();
        result.Value!.QuantityAvailable.Should().Be(14);

        var inventory = await db.Inventory.FindAsync(7);
        inventory!.QuantityAvailable.Should().Be(14);

        var log = await db.StockUpdateLogs.SingleAsync();
        log.ProductId.Should().Be(7);
        log.QuantityDelta.Should().Be(4);
        log.ResultingQuantity.Should().Be(14);
        log.Reason.Should().Be("AdminAdjustment");

        queuePublisher.Verify(q => q.EnqueueAsync(
            It.Is<StockUpdateMessage>(message =>
                message.ProductId == 7 &&
                message.QuantityDelta == 4 &&
                message.Reason == "AdminAdjustment" &&
                message.Notes == "Admin inventory quantity update"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
