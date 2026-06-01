using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OutdoorsShop.Api.Tests.Integration;

public class CustomersIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public CustomersIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetById_ReturnsStableNullAvatarFields_WhenAvatarIsMissing()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await ResetAvatarAsync("customer@test.com");
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        var response = await client.GetAsync($"/api/v1/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        profile.Should().NotBeNull();
        profile!.AvatarPath.Should().BeNull();
        profile.AvatarContentType.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadAvatar_Returns401_WhenUnauthenticated()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var customerId = await GetCustomerIdAsync("customer@test.com");
        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.png");

        var response = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CustomerAvatarFlow_UploadGetAndRemove_PersistsAvatarState()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        var initialProfileResponse = await client.GetAsync($"/api/v1/customers/{customerId}");
        initialProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initialProfile = await initialProfileResponse.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        initialProfile.Should().NotBeNull();
        initialProfile!.AvatarPath.Should().BeNull();
        initialProfile.AvatarContentType.Should().BeNull();
        initialProfile.AvatarUrl.Should().BeNull();

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.png");

        var uploadResponse = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploadedProfile = await uploadResponse.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        uploadedProfile.Should().NotBeNull();
        uploadedProfile!.AvatarPath.Should().Be($"customers/{customerId}/avatar.png");
        uploadedProfile.AvatarContentType.Should().Be("image/png");
        uploadedProfile.AvatarUrl.Should().Be($"https://test.blob.core.windows.net/customer-avatars/customers/{customerId}/avatar.png");

        var getResponse = await client.GetAsync($"/api/v1/customers/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedProfile = await getResponse.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        fetchedProfile.Should().NotBeNull();
        fetchedProfile!.AvatarPath.Should().Be($"customers/{customerId}/avatar.png");
        fetchedProfile.AvatarUrl.Should().Be($"https://test.blob.core.windows.net/customer-avatars/customers/{customerId}/avatar.png");

        var removeResponse = await client.DeleteAsync($"/api/v1/customers/{customerId}/avatar");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var removedProfile = await removeResponse.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        removedProfile.Should().NotBeNull();
        removedProfile!.AvatarPath.Should().BeNull();
        removedProfile.AvatarContentType.Should().BeNull();
        removedProfile.AvatarUrl.Should().BeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutdoorsShop.Infrastructure.Data.AppDbContext>();
        var customer = await db.Customers.IgnoreQueryFilters().SingleAsync(c => c.CustomerID == customerId);
        customer.AvatarPath.Should().BeNull();
        customer.AvatarContentType.Should().BeNull();
    }

    [Fact]
    public async Task UploadAvatar_Returns403_WhenCustomerTargetsAnotherProfile()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var otherCustomerId = await GetCustomerIdAsync("admin@test.com");

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.png");

        var response = await client.PostAsync($"/api/v1/customers/{otherCustomerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadAvatar_Returns200_WhenAdministratorTargetsAnotherProfile()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.png");

        var response = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        profile.Should().NotBeNull();
        profile!.CustomerID.Should().Be(customerId);
        profile.AvatarPath.Should().Be($"customers/{customerId}/avatar.png");

        var cleanupResponse = await client.DeleteAsync($"/api/v1/customers/{customerId}/avatar");
        cleanupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadAvatar_CanonicalizesStoredExtension_FromApprovedContentType()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await ResetAvatarAsync("customer@test.com");
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.exe");

        var response = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        profile.Should().NotBeNull();
        profile!.AvatarPath.Should().Be($"customers/{customerId}/avatar.png");
        profile.AvatarContentType.Should().Be("image/png");
        profile.AvatarUrl.Should().Be($"https://test.blob.core.windows.net/customer-avatars/customers/{customerId}/avatar.png");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutdoorsShop.Infrastructure.Data.AppDbContext>();
        var customer = await db.Customers.IgnoreQueryFilters().SingleAsync(c => c.CustomerID == customerId);
        customer.AvatarPath.Should().Be($"customers/{customerId}/avatar.png");
        customer.AvatarContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task UploadAvatar_Returns400_WhenFileTypeIsInvalid()
    {
        using var client = _factory.CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent("not-an-image"u8.ToArray());
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(avatarContent, "file", "avatar.txt");

        var response = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Invalid avatar file type");
    }

    [Fact]
    public async Task UploadAvatar_Returns400_WhenFileExceedsLimit()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await ResetAvatarAsync("customer@test.com");
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        using var uploadContent = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent(new byte[(2 * 1024 * 1024) + 1]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(avatarContent, "file", "avatar.png");

        var response = await client.PostAsync($"/api/v1/customers/{customerId}/avatar", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("2 MB limit");

        var profileResponse = await client.GetAsync($"/api/v1/customers/{customerId}");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        profile.Should().NotBeNull();
        profile!.AvatarPath.Should().BeNull();
        profile.AvatarContentType.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAvatar_Returns200_WhenAvatarIsAlreadyMissing()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await ResetAvatarAsync("customer@test.com");
        var token = await _factory.GetAuthTokenAsync(client, "customer@test.com", "Customer1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await GetCustomerIdAsync("customer@test.com");

        var response = await client.DeleteAsync($"/api/v1/customers/{customerId}/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerAvatarResponse>();
        profile.Should().NotBeNull();
        profile!.AvatarPath.Should().BeNull();
        profile.AvatarContentType.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
    }

    private async Task ResetAvatarAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutdoorsShop.Infrastructure.Data.AppDbContext>();
        var customer = await db.Customers.IgnoreQueryFilters().SingleAsync(c => c.Email == email);
        customer.AvatarPath = null;
        customer.AvatarContentType = null;
        await db.SaveChangesAsync();
    }

    private async Task<int> GetCustomerIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutdoorsShop.Infrastructure.Data.AppDbContext>();
        return await db.Customers.IgnoreQueryFilters()
            .Where(customer => customer.Email == email)
            .Select(customer => customer.CustomerID)
            .SingleAsync();
    }

    private sealed class CustomerAvatarResponse
    {
        public int CustomerID { get; set; }
        public string? AvatarPath { get; set; }
        public string? AvatarContentType { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
