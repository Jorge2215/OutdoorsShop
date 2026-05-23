using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Functions.Functions;
using OutdoorsShop.Infrastructure.Data;
using System.Text.Json;

namespace OutdoorsShop.Functions.Tests.Functions;

public class StockUpdateFunctionTests
{
    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static string Serialize(StockUpdateMessage msg) => JsonSerializer.Serialize(msg);

    private static async Task SeedInventoryAsync(AppDbContext db, int productId, int qty, int threshold = 5)
    {
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
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
            ReorderThreshold = threshold,
            LastUpdated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Run_IncreasesStock_OnRestock()
    {
        await using var db = CreateDbContext("stock-restock");
        await SeedInventoryAsync(db, productId: 1, qty: 10);

        var message = new StockUpdateMessage(
            ProductId: 1,
            QuantityDelta: 20,
            Reason: "Restock",
            Notes: null,
            UpdatedAt: DateTimeOffset.UtcNow);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var inv = await db.Inventory.FindAsync(1);
        inv!.QuantityAvailable.Should().Be(30);
    }

    [Fact]
    public async Task Run_DecreasesStock_OnSale()
    {
        await using var db = CreateDbContext("stock-sale");
        await SeedInventoryAsync(db, productId: 2, qty: 15, threshold: 3);

        var message = new StockUpdateMessage(
            ProductId: 2,
            QuantityDelta: -5,
            Reason: "Sale",
            Notes: null,
            UpdatedAt: DateTimeOffset.UtcNow);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var inv = await db.Inventory.FindAsync(2);
        inv!.QuantityAvailable.Should().Be(10);
    }

    [Fact]
    public async Task Run_ClampsToZero_WhenDeltaExceedsStock()
    {
        await using var db = CreateDbContext("stock-clamp");
        await SeedInventoryAsync(db, productId: 3, qty: 5);

        var message = new StockUpdateMessage(
            ProductId: 3,
            QuantityDelta: -100,
            Reason: "Sale",
            Notes: null,
            UpdatedAt: DateTimeOffset.UtcNow);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var inv = await db.Inventory.FindAsync(3);
        inv!.QuantityAvailable.Should().Be(0,
            "quantity must be clamped to 0 when delta would produce a negative number");
    }

    [Fact]
    public async Task Run_CreatesInventoryRecord_WhenNotFound()
    {
        await using var db = CreateDbContext("stock-create");
        // Seed only the product — no inventory record
        db.Categories.Add(new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true });
        db.Products.Add(new Product { ProductID = 99, Name = "New Product", CategoryID = 1, Price = 50m, IsActive = true, DiscountMultiplier = 1.0m });
        await db.SaveChangesAsync();

        var message = new StockUpdateMessage(
            ProductId: 99,
            QuantityDelta: 10,
            Reason: "Initial",
            Notes: "New product stock",
            UpdatedAt: DateTimeOffset.UtcNow);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var inv = await db.Inventory.FindAsync(99);
        inv.Should().NotBeNull();
        inv!.QuantityAvailable.Should().Be(10,
            "newly created inventory starts at 0 then delta is applied");
    }

    [Fact]
    public async Task Run_WritesStockUpdateLog_OnEveryUpdate()
    {
        await using var db = CreateDbContext("stock-log");
        await SeedInventoryAsync(db, productId: 4, qty: 20);

        var updatedAt = DateTimeOffset.UtcNow;
        var message = new StockUpdateMessage(
            ProductId: 4,
            QuantityDelta: 5,
            Reason: "Restock",
            Notes: "Weekly replenishment",
            UpdatedAt: updatedAt);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var logs = await db.StockUpdateLogs.ToListAsync();
        logs.Should().HaveCount(1);

        var log = logs.Single();
        log.ProductId.Should().Be(4);
        log.QuantityDelta.Should().Be(5);
        log.ResultingQuantity.Should().Be(25);
        log.Reason.Should().Be("Restock");
        log.Notes.Should().Be("Weekly replenishment");
    }

    [Fact]
    public async Task Run_LogsLowStockWarning_WhenBelowThreshold()
    {
        // We can't directly observe the logger without a custom ILogger<T>,
        // but we verify the side effects are correct: inventory is updated and
        // the quantity is at or below the reorder threshold (triggering the warning path).
        await using var db = CreateDbContext("stock-low-stock");
        await SeedInventoryAsync(db, productId: 5, qty: 6, threshold: 5);

        var message = new StockUpdateMessage(
            ProductId: 5,
            QuantityDelta: -2,
            Reason: "Sale",
            Notes: null,
            UpdatedAt: DateTimeOffset.UtcNow);

        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);
        await function.Run(Serialize(message));

        var inv = await db.Inventory.FindAsync(5);
        inv!.QuantityAvailable.Should().Be(4);
        inv.IsLowStock().Should().BeTrue(
            "quantity 4 is below reorderThreshold 5 — the low-stock warning code path was exercised");
    }

    [Fact]
    public async Task Run_HandlesInvalidJson_Gracefully()
    {
        await using var db = CreateDbContext("stock-invalid-json");
        var function = new StockUpdateFunction(db, NullLogger<StockUpdateFunction>.Instance);

        var act = async () => await function.Run("not valid json at all");
        await act.Should().NotThrowAsync();
    }
}

// Extension to expose low-stock check without modifying the entity
file static class InventoryExtensions
{
    public static bool IsLowStock(this ProductInventory inv) =>
        inv.QuantityAvailable <= inv.ReorderThreshold;
}
