using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutdoorsShop.Api.Tests.Integration;

public class OrderReceiptsIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public OrderReceiptsIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetReceipt_Returns401_WhenUnauthenticated()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/orders/1/receipt");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReceipt_Returns200_WithReceiptMetadata_ForOwningCustomer()
    {
        using var client = CreateClient();
        var email = $"receipt-owner-{Guid.NewGuid():N}@test.com";
        const string password = "ReceiptPass123!";
        var token = await RegisterUserAsync(client, email, password);
        var orderId = await SeedOrderAsync(email, PaymentStatus.Confirmed);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/v1/orders/{orderId}/receipt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<OrderReceiptDto>();
        payload.Should().NotBeNull();
        payload!.OrderID.Should().Be(orderId);
        payload.ReceiptAvailable.Should().BeTrue();
        payload.DownloadUrl.Should().StartWith("https://test.blob.core.windows.net/test/blob");
    }

    [Fact]
    public async Task GetReceipt_Returns403_WhenCustomerRequestsAnotherCustomersReceipt()
    {
        using var client = CreateClient();
        var ownerEmail = $"receipt-owner-{Guid.NewGuid():N}@test.com";
        var attackerEmail = $"receipt-attacker-{Guid.NewGuid():N}@test.com";
        const string password = "ReceiptPass123!";

        await RegisterUserAsync(client, ownerEmail, password);
        var attackerToken = await RegisterUserAsync(client, attackerEmail, password);
        var orderId = await SeedOrderAsync(ownerEmail, PaymentStatus.Confirmed);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var response = await client.GetAsync($"/api/v1/orders/{orderId}/receipt");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReceipt_Returns404_WhenOrderDoesNotExist()
    {
        using var client = CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/orders/999999/receipt");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private static async Task<string> RegisterUserAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Receipt Integration User",
            email,
            password,
            confirmPassword = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<int> SeedOrderAsync(string customerEmail, PaymentStatus paymentStatus)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = await db.Customers.SingleAsync(c => c.Email == customerEmail);
        var product = await db.Products.SingleAsync(p => p.ProductID == 1);

        var order = new SalesOrder
        {
            CustomerID = customer.CustomerID,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = "42 Receipt Ridge",
            PaymentMethod = "CreditCard",
            TotalAmount = product.Price,
            Status = OrderStatus.Processing,
            PaymentStatus = paymentStatus,
            PaymentReference = paymentStatus == PaymentStatus.Confirmed ? $"PAY-{Guid.NewGuid():N}" : null,
            PaidAt = paymentStatus == PaymentStatus.Confirmed ? DateTimeOffset.UtcNow : null,
            Details =
            [
                new SalesOrderDetail
                {
                    ProductID = product.ProductID,
                    Quantity = 1,
                    UnitPrice = product.Price
                }
            ]
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.OrderID;
    }
}
