using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Functions.Functions;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Functions.Tests.Functions;

/// <summary>
/// SeasonalDiscountFunction uses TimeProvider for date abstraction (.NET 8+).
/// FakeTimeProvider subclasses TimeProvider to pin dates for season-specific tests.
/// </summary>
public class SeasonalDiscountFunctionTests
{
    private static AppDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    // CategoryID constants matching AppDbContext seed data
    private const int CampingCategoryId = 1;
    private const int TrekkingCategoryId = 2;
    private const int CyclingCategoryId = 3;
    private const int ClimbingCategoryId = 4;

    private static decimal ExpectedMultiplierForMonth(int month, int categoryId)
    {
        bool isWinter = month is 12 or 1 or 2;
        bool isSummer = month is 6 or 7 or 8;
        bool isWinterCategory = categoryId is CampingCategoryId or TrekkingCategoryId;
        bool isSummerCategory = categoryId is CyclingCategoryId or ClimbingCategoryId;

        return (isWinter && isWinterCategory) ? 0.85m
             : (isSummer && isSummerCategory) ? 0.90m
             : 1.0m;
    }

    /// <summary>
    /// Seeds one product per category and verifies all multipliers match the expected
    /// value for whatever season it currently is. This test is deterministic regardless
    /// of when it runs.
    /// </summary>
    [Fact]
    public async Task Run_SetsCorrectMultipliersForCurrentSeason()
    {
        await using var db = CreateDbContext("seasonal-current-season");

        // Seed categories (InMemory does not apply HasData seed, so insert manually)
        db.Categories.AddRange(
            new ProductCategory { CategoryID = CampingCategoryId, Name = "Camping", IsActive = true },
            new ProductCategory { CategoryID = TrekkingCategoryId, Name = "Trekking", IsActive = true },
            new ProductCategory { CategoryID = CyclingCategoryId, Name = "Cycling", IsActive = true },
            new ProductCategory { CategoryID = ClimbingCategoryId, Name = "Climbing", IsActive = true });

        db.Products.AddRange(
            new Product { ProductID = 1, Name = "Tent", CategoryID = CampingCategoryId, Price = 100m, IsActive = true, DiscountMultiplier = 1.0m },
            new Product { ProductID = 2, Name = "Boots", CategoryID = TrekkingCategoryId, Price = 80m, IsActive = true, DiscountMultiplier = 1.0m },
            new Product { ProductID = 3, Name = "Bike", CategoryID = CyclingCategoryId, Price = 500m, IsActive = true, DiscountMultiplier = 1.0m },
            new Product { ProductID = 4, Name = "Harness", CategoryID = ClimbingCategoryId, Price = 120m, IsActive = true, DiscountMultiplier = 1.0m });
        await db.SaveChangesAsync();

        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance);
        await function.Run(null!);

        int currentMonth = DateTime.UtcNow.Month;

        var products = await db.Products.ToListAsync();
        foreach (var product in products)
        {
            var expected = ExpectedMultiplierForMonth(currentMonth, product.CategoryID);
            product.DiscountMultiplier.Should().Be(expected,
                $"Product {product.ProductID} (category {product.CategoryID}) in month {currentMonth}");
        }
    }

    [Fact]
    public async Task Run_OnlyAffectsActiveProducts_InactiveProductNotModified()
    {
        await using var db = CreateDbContext("seasonal-active-only");

        db.Categories.Add(new ProductCategory { CategoryID = CampingCategoryId, Name = "Camping", IsActive = true });
        // Active product
        db.Products.Add(new Product { ProductID = 1, Name = "Active Tent", CategoryID = CampingCategoryId, Price = 100m, IsActive = true, DiscountMultiplier = 1.0m });
        // Inactive product — stored directly, bypassing EF query filter
        await db.SaveChangesAsync();

        // Insert inactive product bypassing the global query filter using raw EF
        db.Database.EnsureCreated();
        db.Products.Add(new Product { ProductID = 2, Name = "Inactive Tent", CategoryID = CampingCategoryId, Price = 100m, IsActive = false, DiscountMultiplier = 1.0m });
        await db.SaveChangesAsync();

        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance);
        await function.Run(null!);

        // Inactive product must not have been touched by the function
        var inactive = await db.Products.IgnoreQueryFilters()
            .FirstAsync(p => p.ProductID == 2);
        inactive.DiscountMultiplier.Should().Be(1.0m,
            "inactive products are filtered out by the global query filter and must not be modified");
    }

    [Fact]
    public async Task Execute_AppliesWinterDiscount_ToTrekkingAndCampingProducts()
    {
        await using var db = CreateDbContext("seasonal-winter");
        SeedAllCategories(db);
        SeedAllProducts(db, startId: 10);
        await db.SaveChangesAsync();

        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 2, 0, 0, TimeSpan.Zero));
        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance, clock);
        await function.Run(null!);

        var products = await db.Products.ToListAsync();
        products.First(p => p.CategoryID == CampingCategoryId).DiscountMultiplier.Should().Be(0.85m);
        products.First(p => p.CategoryID == TrekkingCategoryId).DiscountMultiplier.Should().Be(0.85m);
        products.First(p => p.CategoryID == CyclingCategoryId).DiscountMultiplier.Should().Be(1.0m);
        products.First(p => p.CategoryID == ClimbingCategoryId).DiscountMultiplier.Should().Be(1.0m);
    }

    [Fact]
    public async Task Execute_AppliesSummerDiscount_ToCyclingAndClimbingProducts()
    {
        await using var db = CreateDbContext("seasonal-summer");
        SeedAllCategories(db);
        SeedAllProducts(db, startId: 20);
        await db.SaveChangesAsync();

        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 15, 2, 0, 0, TimeSpan.Zero));
        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance, clock);
        await function.Run(null!);

        var products = await db.Products.ToListAsync();
        products.First(p => p.CategoryID == CampingCategoryId).DiscountMultiplier.Should().Be(1.0m);
        products.First(p => p.CategoryID == TrekkingCategoryId).DiscountMultiplier.Should().Be(1.0m);
        products.First(p => p.CategoryID == CyclingCategoryId).DiscountMultiplier.Should().Be(0.90m);
        products.First(p => p.CategoryID == ClimbingCategoryId).DiscountMultiplier.Should().Be(0.90m);
    }

    [Fact]
    public async Task Execute_ResetsDiscount_InSpring()
    {
        await using var db = CreateDbContext("seasonal-spring");
        SeedAllCategories(db);
        SeedAllProducts(db, startId: 30, initialMultiplier: 0.85m);
        await db.SaveChangesAsync();

        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 2, 0, 0, TimeSpan.Zero));
        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance, clock);
        await function.Run(null!);

        var products = await db.Products.ToListAsync();
        products.Should().AllSatisfy(p =>
            p.DiscountMultiplier.Should().Be(1.0m, $"Spring resets all multipliers to 1.0 (product {p.ProductID})"));
    }

    [Fact]
    public async Task Execute_ResetsDiscount_InAutumn()
    {
        await using var db = CreateDbContext("seasonal-autumn");
        SeedAllCategories(db);
        SeedAllProducts(db, startId: 40, initialMultiplier: 0.90m);
        await db.SaveChangesAsync();

        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 2, 0, 0, TimeSpan.Zero));
        var function = new SeasonalDiscountFunction(db, NullLogger<SeasonalDiscountFunction>.Instance, clock);
        await function.Run(null!);

        var products = await db.Products.ToListAsync();
        products.Should().AllSatisfy(p =>
            p.DiscountMultiplier.Should().Be(1.0m, $"Autumn resets all multipliers to 1.0 (product {p.ProductID})"));
    }

    private static void SeedAllCategories(AppDbContext db)
    {
        db.Categories.AddRange(
            new ProductCategory { CategoryID = CampingCategoryId, Name = "Camping", IsActive = true },
            new ProductCategory { CategoryID = TrekkingCategoryId, Name = "Trekking", IsActive = true },
            new ProductCategory { CategoryID = CyclingCategoryId, Name = "Cycling", IsActive = true },
            new ProductCategory { CategoryID = ClimbingCategoryId, Name = "Climbing", IsActive = true });
    }

    private static void SeedAllProducts(AppDbContext db, int startId, decimal initialMultiplier = 1.0m)
    {
        db.Products.AddRange(
            new Product { ProductID = startId, Name = "Tent", CategoryID = CampingCategoryId, Price = 100m, IsActive = true, DiscountMultiplier = initialMultiplier },
            new Product { ProductID = startId + 1, Name = "Boots", CategoryID = TrekkingCategoryId, Price = 80m, IsActive = true, DiscountMultiplier = initialMultiplier },
            new Product { ProductID = startId + 2, Name = "Bike", CategoryID = CyclingCategoryId, Price = 500m, IsActive = true, DiscountMultiplier = initialMultiplier },
            new Product { ProductID = startId + 3, Name = "Harness", CategoryID = ClimbingCategoryId, Price = 120m, IsActive = true, DiscountMultiplier = initialMultiplier });
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
