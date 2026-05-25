using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutdoorsShop.Api.Tests.Integration;

/// <summary>
/// Contract tests for the upcoming change-password endpoint.
/// </summary>
public class ChangePasswordIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public ChangePasswordIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangePassword_Returns200_AndInvalidatesOldPassword_WhenRequestIsValid()
    {
        using var client = CreateClient();
        var email = $"change-password-happy-{Guid.NewGuid():N}@test.com";
        const string currentPassword = "CurrentPass123";
        const string newPassword = "UpdatedPass123";

        var accessToken = await RegisterUserAsync(client, email, currentPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword,
            newPassword,
            confirmNewPassword = newPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the requested API contract is PUT /api/v1/users/change-password");

        var oldPasswordLogin = await LoginAsync(email, currentPassword);
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await LoginAsync(email, newPassword);
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenCurrentPasswordIsWrong()
    {
        using var client = CreateClient();
        var email = $"change-password-wrong-current-{Guid.NewGuid():N}@test.com";
        const string currentPassword = "CurrentPass123";
        const string newPassword = "UpdatedPass123";

        var accessToken = await RegisterUserAsync(client, email, currentPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword = "WrongPass123",
            newPassword,
            confirmNewPassword = newPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("current");
    }

    [Fact]
    public async Task ChangePassword_Returns401_WhenUnauthenticated()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword = "CurrentPass123",
            newPassword = "UpdatedPass123",
            confirmNewPassword = "UpdatedPass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenConfirmationDoesNotMatch()
    {
        using var client = CreateClient();
        var email = $"change-password-mismatch-{Guid.NewGuid():N}@test.com";
        const string currentPassword = "CurrentPass123";

        var accessToken = await RegisterUserAsync(client, email, currentPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword,
            newPassword = "UpdatedPass123",
            confirmNewPassword = "MismatchPass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("confirm");
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenNewPasswordIsTooShort()
    {
        using var client = CreateClient();
        var email = $"change-password-short-{Guid.NewGuid():N}@test.com";
        const string currentPassword = "CurrentPass123";

        var accessToken = await RegisterUserAsync(client, email, currentPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword,
            newPassword = "Short1",
            confirmNewPassword = "Short1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("8");
    }

    [Fact]
    public async Task ChangePassword_DoesNotAffectOtherUsers_WhenRequestIsValid()
    {
        using var client = CreateClient();
        var firstUserEmail = $"change-password-primary-{Guid.NewGuid():N}@test.com";
        var secondUserEmail = $"change-password-secondary-{Guid.NewGuid():N}@test.com";
        const string currentPassword = "CurrentPass123";
        const string newPassword = "UpdatedPass123";
        const string secondUserPassword = "SecondPass123";

        var accessToken = await RegisterUserAsync(client, firstUserEmail, currentPassword);
        await RegisterUserAsync(client, secondUserEmail, secondUserPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            currentPassword,
            newPassword,
            confirmNewPassword = newPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the requested API contract is PUT /api/v1/users/change-password");

        var otherUserLogin = await LoginAsync(secondUserEmail, secondUserPassword);
        otherUserLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<string> RegisterUserAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Change Password Test User",
            email,
            password,
            confirmPassword = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        using var client = CreateClient();
        return await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });
    }
}
