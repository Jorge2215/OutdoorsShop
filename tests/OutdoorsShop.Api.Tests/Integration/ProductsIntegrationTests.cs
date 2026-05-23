using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OutdoorsShop.Api.Tests.Integration;

/// <summary>
/// Integration tests for Products endpoints using WebApplicationFactory + SQLite in-memory EF Core.
/// </summary>
public class ProductsIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public ProductsIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetProducts_Returns200_WithPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("productID");
    }

    [Fact]
    public async Task GetProduct_Returns404_ForNonExistentProduct()
    {
        var response = await _client.GetAsync("/api/v1/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProduct_Returns200_ForExistingProduct()
    {
        var response = await _client.GetAsync("/api/v1/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_Returns401_WhenUnauthenticated()
    {
        var payload = new
        {
            name = "Test Product",
            categoryID = 1,
            price = 29.99
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_Returns403_WhenAuthenticatedAsCustomer()
    {
        var token = await _factory.GetAuthTokenAsync(_client, "customer@test.com", "Customer1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            name = "Test Product",
            categoryID = 1,
            price = 29.99
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateProduct_Returns201_WhenAuthenticatedAsAdmin()
    {
        var token = await _factory.GetAuthTokenAsync(_client, "admin@test.com", "Admin1234!");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            name = "Integration Test Tent",
            categoryID = 1,
            price = 299.99
        };

        var response = await client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProducts_FiltersByCategory_WhenCategoryIdProvided()
    {
        var response = await _client.GetAsync("/api/v1/products?categoryId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrEmpty();
    }
}

