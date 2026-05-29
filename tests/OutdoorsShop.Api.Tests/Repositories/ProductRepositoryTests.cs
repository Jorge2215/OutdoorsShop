using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Repositories;

namespace OutdoorsShop.Api.Tests.Repositories;

public class ProductRepositoryTests
{
    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SearchProductsAsync_ComposesSearchCategoryAndPriceFiltersWithAndLogic()
    {
        await using var db = CreateDbContext(nameof(SearchProductsAsync_ComposesSearchCategoryAndPriceFiltersWithAndLogic));
        await SeedCatalogAsync(db);
        var repository = new ProductRepository(db);

        var products = (await repository.SearchProductsAsync("tent", 1, 100m, 200m, "price_desc")).ToList();

        products.Should().ContainSingle();
        products[0].Name.Should().Be("Alpine Tent");
    }

    [Fact]
    public async Task SearchProductsAsync_FallsBackToNameAscending_WhenSortIsInvalid()
    {
        await using var db = CreateDbContext(nameof(SearchProductsAsync_FallsBackToNameAscending_WhenSortIsInvalid));
        await SeedCatalogAsync(db);
        var repository = new ProductRepository(db);

        var products = (await repository.SearchProductsAsync(null, 1, null, null, "popular")).ToList();

        products.Select(p => p.Name).Should().ContainInOrder("Alpine Tent", "Camp Mug", "Summit Tent");
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsEmpty_WhenMinPriceExceedsMaxPrice()
    {
        await using var db = CreateDbContext(nameof(SearchProductsAsync_ReturnsEmpty_WhenMinPriceExceedsMaxPrice));
        await SeedCatalogAsync(db);
        var repository = new ProductRepository(db);

        var products = await repository.SearchProductsAsync(null, null, 300m, 100m, "price_asc");

        products.Should().BeEmpty();
    }

    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        db.Categories.AddRange(
            new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true },
            new ProductCategory { CategoryID = 2, Name = "Trekking", IsActive = true });

        db.Products.AddRange(
            new Product
            {
                ProductID = 1,
                Name = "Summit Tent",
                CategoryID = 1,
                Price = 220m,
                Description = "Four-season tent",
                IsActive = true,
                DiscountMultiplier = 1.0m
            },
            new Product
            {
                ProductID = 2,
                Name = "Alpine Tent",
                CategoryID = 1,
                Price = 150m,
                Description = "Lightweight tent for alpine weather",
                IsActive = true,
                DiscountMultiplier = 1.0m
            },
            new Product
            {
                ProductID = 3,
                Name = "Trail Boots",
                CategoryID = 2,
                Price = 130m,
                Description = "Waterproof hiking boots",
                IsActive = true,
                DiscountMultiplier = 1.0m
            },
            new Product
            {
                ProductID = 4,
                Name = "Camp Mug",
                CategoryID = 1,
                Price = 25m,
                Description = "Steel mug",
                IsActive = true,
                DiscountMultiplier = 1.0m
            });

        await db.SaveChangesAsync();
    }
}
