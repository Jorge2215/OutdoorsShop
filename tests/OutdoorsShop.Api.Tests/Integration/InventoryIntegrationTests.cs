using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutdoorsShop.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutdoorsShop.Api.Tests.Integration;

public class InventoryIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public InventoryIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetInventoryByProductId_CreatesDefaultInventory_ForLegacyProductWithoutStockRow()
    {
        const int productId = 23;
        await SeedProductWithoutInventoryAsync(productId, "Legacy Bike Pump");

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/v1/inventory/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("productID").GetInt32().Should().Be(productId);
        document.RootElement.GetProperty("quantityAvailable").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("reorderThreshold").GetInt32().Should().Be(5);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inventory = await db.Inventory.FindAsync(productId);
        inventory.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateInventory_CreatesMissingRow_BeforeApplyingAdminStockChange()
    {
        const int productId = 24;
        await SeedProductWithoutInventoryAsync(productId, "Legacy Repair Kit");

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/api/v1/inventory/{productId}", new
        {
            quantityAvailable = 7
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("quantityAvailable").GetInt32().Should().Be(7);
        document.RootElement.GetProperty("reorderThreshold").GetInt32().Should().Be(5);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inventory = await db.Inventory.FindAsync(productId);
        inventory.Should().NotBeNull();
        inventory!.QuantityAvailable.Should().Be(7);
    }

    private async Task SeedProductWithoutInventoryAsync(int productId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Products.IgnoreQueryFilters().AnyAsync(product => product.ProductID == productId))
            return;

        db.Products.Add(new OutdoorsShop.Core.Entities.Product
        {
            ProductID = productId,
            Name = name,
            CategoryID = 3,
            Price = 29.99m,
            IsActive = true,
            DiscountMultiplier = 1.0m
        });

        await db.SaveChangesAsync();
    }
}
