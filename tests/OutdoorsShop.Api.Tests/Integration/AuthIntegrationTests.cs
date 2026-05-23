using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutdoorsShop.Api.Tests.Integration;

/// <summary>
/// Integration tests for Auth endpoints using WebApplicationFactory + SQLite in-memory EF Core.
/// </summary>
public class AuthIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AuthIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsOk_WithAccessToken()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            name = "New Integration User",
            email = $"newuser-{Guid.NewGuid():N}@test.com",
            password = "NewUser1234!",
            confirmPassword = "NewUser1234!"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ReturnsOk_WithValidCredentials()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "customer@test.com",
            password = "Customer1234!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns401_WithInvalidCredentials()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "customer@test.com",
            password = "WrongPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_RegisterLoginAndCallProtectedEndpoint()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"flow-{Guid.NewGuid():N}@test.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Flow Test User",
            email = uniqueEmail,
            password = "FlowTest1234!",
            confirmPassword = "FlowTest1234!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = uniqueEmail,
            password = "FlowTest1234!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(loginJson).RootElement
            .GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResponse = await client.GetAsync("/api/v1/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meJson = await meResponse.Content.ReadAsStringAsync();
        var meDoc = JsonDocument.Parse(meJson);
        meDoc.RootElement.GetProperty("email").GetString().Should().Be(uniqueEmail);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WhenNoToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WhenTokenExpiredOrInvalid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt.token");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

