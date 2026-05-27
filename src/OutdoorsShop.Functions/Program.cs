using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;
using OutdoorsShop.Infrastructure.Repositories;
using OutdoorsShop.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// EF Core — read connection string from environment (FUNCTIONS_WORKER_RUNTIME / local.settings.json)
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"]
    ?? "Server=.;Database=OutdoorsShopDb;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton(TimeProvider.System);

var storageConnectionString = builder.Configuration["AzureStorage:ConnectionString"]
    ?? builder.Configuration["AzureWebJobsStorage"]
    ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IReceiptQueuePublisher, ReceiptQueuePublisher>();

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();
