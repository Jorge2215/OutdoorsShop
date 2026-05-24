using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Identity;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutdoorsShop.Api.Tests.Integration;

/// <summary>
/// WebApplicationFactory that replaces SQL Server with a SQLite in-memory database
/// and seeds test data for integration tests.
///
/// EF Core 10.0 multi-provider fix:
///   - Connection string is blanked via UseSetting so AddDatabase in Program.cs
///     skips UseSqlServer registration entirely (no SQL Server in the DI container).
///   - This factory then registers UseSqlite as the sole provider.
///   - Schema creation and seeding occur in CreateHost, after the final service
///     provider is fully built (so Identity services are available for user seeding).
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    // Keep the connection open for the factory lifetime — SQLite in-memory
    // databases are destroyed when their connection closes.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        // Blank the DB connection string → AddDatabase in Program.cs hits the null-guard
        // and skips UseSqlServer, so only the SQLite provider registered below is present.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "");

        // Blank blob connection string → AddBlobStorage falls back to UseDevelopmentStorage=true
        // (valid Azurite format that doesn't require a running emulator).
        builder.UseSetting("AzureStorage:ConnectionString", "");

        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Register SQLite in-memory as the sole EF Core provider.
            // Program.cs's AddDatabase has already been skipped (empty connection string),
            // so there is no SQL Server provider to conflict with.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Replace blob storage with a no-op mock to avoid requiring Azure Storage emulator.
            services.RemoveAll<BlobServiceClient>();
            services.RemoveAll<IBlobStorageService>();
            var blobMock = new Mock<IBlobStorageService>();
            blobMock.Setup(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("https://test.blob.core.windows.net/test/blob");
            blobMock.Setup(b => b.GetSasUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync("https://test.blob.core.windows.net/test/blob?sas=token");
            blobMock.Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            blobMock.Setup(b => b.UploadProductImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("https://test.blob.core.windows.net/product-images/products/1/test.jpg");
            services.AddSingleton(blobMock.Object);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create schema and seed data AFTER the host is fully built so Identity services
        // (UserManager, RoleManager) are available in the service provider.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // EnsureCreated builds the schema from the EF model — correct for SQLite test environments.
        // Do NOT use Migrate() — SQLite doesn't support all SQL Server migration syntax.
        db.Database.EnsureCreated();
        SeedTestData(scope.ServiceProvider).GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    private static async Task SeedTestData(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles
        foreach (var role in new[] { "Administrator", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed categories (SQLite does not apply HasData seed, so insert manually)
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new ProductCategory { CategoryID = 1, Name = "Camping", IsActive = true },
                new ProductCategory { CategoryID = 2, Name = "Trekking", IsActive = true },
                new ProductCategory { CategoryID = 3, Name = "Cycling", IsActive = true },
                new ProductCategory { CategoryID = 4, Name = "Climbing", IsActive = true });
            await db.SaveChangesAsync();
        }

        // Seed 5 products
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { ProductID = 1, Name = "Camping Tent", CategoryID = 1, Price = 149.99m, IsActive = true, DiscountMultiplier = 1.0m },
                new Product { ProductID = 2, Name = "Trekking Boots", CategoryID = 2, Price = 89.99m, IsActive = true, DiscountMultiplier = 1.0m },
                new Product { ProductID = 3, Name = "Road Bike", CategoryID = 3, Price = 599.99m, IsActive = true, DiscountMultiplier = 1.0m },
                new Product { ProductID = 4, Name = "Climbing Harness", CategoryID = 4, Price = 79.99m, IsActive = true, DiscountMultiplier = 1.0m },
                new Product { ProductID = 5, Name = "Sleeping Bag", CategoryID = 1, Price = 59.99m, IsActive = true, DiscountMultiplier = 1.0m });

            db.Inventory.AddRange(
                new ProductInventory { ProductID = 1, QuantityAvailable = 20, ReorderThreshold = 5, LastUpdated = DateTime.UtcNow },
                new ProductInventory { ProductID = 2, QuantityAvailable = 15, ReorderThreshold = 5, LastUpdated = DateTime.UtcNow },
                new ProductInventory { ProductID = 3, QuantityAvailable = 8, ReorderThreshold = 3, LastUpdated = DateTime.UtcNow },
                new ProductInventory { ProductID = 4, QuantityAvailable = 25, ReorderThreshold = 5, LastUpdated = DateTime.UtcNow },
                new ProductInventory { ProductID = 5, QuantityAvailable = 30, ReorderThreshold = 10, LastUpdated = DateTime.UtcNow });

            await db.SaveChangesAsync();
        }

        // Seed admin user
        if (await userManager.FindByEmailAsync("admin@test.com") is null)
        {
            var admin = new ApplicationUser { UserName = "admin@test.com", Email = "admin@test.com" };
            var result = await userManager.CreateAsync(admin, "Admin1234!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrator");
                db.Customers.Add(new Customer
                {
                    UserId = admin.Id,
                    Name = "Test Admin",
                    Email = "admin@test.com",
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }

        // Seed customer user
        if (await userManager.FindByEmailAsync("customer@test.com") is null)
        {
            var customer = new ApplicationUser { UserName = "customer@test.com", Email = "customer@test.com" };
            var result = await userManager.CreateAsync(customer, "Customer1234!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, "Customer");
                db.Customers.Add(new Customer
                {
                    UserId = customer.Id,
                    Name = "Test Customer",
                    Email = "customer@test.com",
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task<string> GetAuthTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }
}

