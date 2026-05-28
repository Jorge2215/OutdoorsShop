using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Messages;
using OutdoorsShop.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OutdoorsShop.Api.Tests.Integration;

public class ReportExportRequestsIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public ReportExportRequestsIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostRequest_Returns202AndPersistsPendingExport()
    {
        using var client = CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/reports/requests", new
        {
            reportType = "orders",
            format = "csv",
            from = "2026-05-01T00:00:00Z",
            to = "2026-05-27T23:59:59Z"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<ReportExportRequestDto>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(ReportExportRequestStatuses.Pending);
        payload.ReportType.Should().Be("orders");
        payload.Format.Should().Be("csv");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ReportExportRequests.SingleAsync(r => r.Id == payload.Id);
        stored.Status.Should().Be(ReportExportRequestStatuses.Pending);
        stored.ReportType.Should().Be("orders");
        stored.Format.Should().Be("csv");
    }

    [Fact]
    public async Task GetRequestById_ReturnsCompletedMetadata()
    {
        using var client = CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var requestId = await SeedExportRequestAsync(ReportExportRequestStatuses.Completed);

        var response = await client.GetAsync($"/api/v1/reports/requests/{requestId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReportExportRequestDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(requestId);
        payload.Status.Should().Be(ReportExportRequestStatuses.Completed);
        payload.DownloadAvailable.Should().BeTrue();
        payload.FileName.Should().Be("orders-report-test.csv");
    }

    [Fact]
    public async Task Download_ReturnsConflict_WhenExportIsNotReady()
    {
        using var client = CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var requestId = await SeedExportRequestAsync(ReportExportRequestStatuses.Processing);

        var response = await client.GetAsync($"/api/v1/reports/requests/{requestId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Download_ReturnsBlobBackedUrl_WhenExportIsComplete()
    {
        using var client = CreateClient();
        var token = await _factory.GetAuthTokenAsync(client, "admin@test.com", "Admin1234!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var requestId = await SeedExportRequestAsync(ReportExportRequestStatuses.Completed);

        var response = await client.GetAsync($"/api/v1/reports/requests/{requestId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReportExportDownloadDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(requestId);
        payload.Status.Should().Be(ReportExportRequestStatuses.Completed);
        payload.DownloadUrl.Should().StartWith("https://test.blob.core.windows.net/test/blob");
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private async Task<Guid> SeedExportRequestAsync(string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = new ReportExportRequest
        {
            Id = Guid.NewGuid(),
            Status = status,
            ReportType = "orders",
            Format = "csv",
            RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ProcessingStartedAt = DateTimeOffset.UtcNow.AddMinutes(-4),
            CompletedAt = status == ReportExportRequestStatuses.Completed ? DateTimeOffset.UtcNow.AddMinutes(-1) : null,
            BlobName = status == ReportExportRequestStatuses.Completed ? "orders/test.csv" : null,
            BlobUrl = status == ReportExportRequestStatuses.Completed ? "https://test.blob.core.windows.net/test/blob" : null,
            FileName = status == ReportExportRequestStatuses.Completed ? "orders-report-test.csv" : null,
            ContentType = status == ReportExportRequestStatuses.Completed ? "text/csv" : null,
            FileSizeBytes = status == ReportExportRequestStatuses.Completed ? 128 : null
        };

        db.ReportExportRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }
}
