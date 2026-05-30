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

    [Fact]
    public async Task GetByProductIdAsync_CreatesDefaultInventory_WhenProductExistsWithoutInventory()
    {
        await using var db = CreateDbContext(nameof(GetByProductIdAsync_CreatesDefaultInventory_WhenProductExistsWithoutInventory));
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product
        {
            ProductID = 23,
            Name = "Imported Pump",
            CategoryID = 1,
            Price = 49m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });
        await db.SaveChangesAsync();

        var queuePublisher = new Mock<IStockUpdateQueuePublisher>();
        var service = new InventoryService(
            new InventoryRepository(db),
            db,
            queuePublisher.Object,
            NullLogger<InventoryService>.Instance);

        var result = await service.GetByProductIdAsync(23);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProductID.Should().Be(23);
        result.Value.QuantityAvailable.Should().Be(0);
        result.Value.ReorderThreshold.Should().Be(5);

        var inventory = await db.Inventory.FindAsync(23);
        inventory.Should().NotBeNull();
        inventory!.QuantityAvailable.Should().Be(0);
        inventory.ReorderThreshold.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_BackfillsMissingInventoryRows_BeforePaging()
    {
        await using var db = CreateDbContext(nameof(GetPagedAsync_BackfillsMissingInventoryRows_BeforePaging));
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.AddRange(
            new Product
            {
                ProductID = 30,
                Name = "Imported Stove",
                CategoryID = 1,
                Price = 79m,
                IsActive = true,
                DiscountMultiplier = 1.0m
            },
            new Product
            {
                ProductID = 31,
                Name = "Imported Lantern",
                CategoryID = 1,
                Price = 39m,
                IsActive = true,
                DiscountMultiplier = 1.0m
            });
        db.Inventory.Add(new ProductInventory
        {
            ProductID = 30,
            QuantityAvailable = 11,
            ReorderThreshold = 4,
            LastUpdated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var queuePublisher = new Mock<IStockUpdateQueuePublisher>();
        var service = new InventoryService(
            new InventoryRepository(db),
            db,
            queuePublisher.Object,
            NullLogger<InventoryService>.Instance);

        var result = await service.GetPagedAsync(1, 20);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.ProductID).Should().Contain([30, 31]);
        result.Items.Single(item => item.ProductID == 31).QuantityAvailable.Should().Be(0);

        var createdInventory = await db.Inventory.FindAsync(31);
        createdInventory.Should().NotBeNull();
        createdInventory!.ReorderThreshold.Should().Be(5);
    }
}
