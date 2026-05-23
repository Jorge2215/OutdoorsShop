# Skill: Injecting EF Core DbContext into Azure Functions Isolated Worker (.NET)

**Date:** 2026-05-23T19:36:12.645-03:00  
**Author:** Cinnamon

---

## Problem

Azure Functions isolated worker (.NET) has its own DI host. You need to inject `DbContext` into function classes so they can query the database — without the ASP.NET Core pipeline.

---

## Pattern

### 1. Reference the Infrastructure project

In `OutdoorsShop.Functions.csproj`:
```xml
<ProjectReference Include="..\OutdoorsShop.Infrastructure\OutdoorsShop.Infrastructure.csproj" />
```
This transitively pulls in EF Core and the `AppDbContext`.

### 2. Register DbContext in `Program.cs` (Functions host)

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Build().Run();
```

> **Why two keys?** `local.settings.json` uses `__` as a separator for nested config: `ConnectionStrings__DefaultConnection`. In production (Azure App Settings), `:` is used. Reading both handles both environments.

### 3. Inject into function class via constructor

```csharp
public class MyFunction
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MyFunction> _logger;

    public MyFunction(AppDbContext dbContext, ILogger<MyFunction> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [Function("MyFunction")]
    public async Task Run([QueueTrigger("my-queue", Connection = "AzureWebJobsStorage")] string message)
    {
        var items = await _dbContext.Products.ToListAsync();
        // ...
        await _dbContext.SaveChangesAsync();
    }
}
```

### 4. `local.settings.json` connection string

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OutdoorsShopDev;Trusted_Connection=True;"
  }
}
```

Note: `ConnectionStrings__DefaultConnection` (double underscore) is how nested sections map in Azure Functions local settings.

---

## EF Core Global Query Filters

If the `DbContext` has `HasQueryFilter` (e.g., `IsActive` soft-delete), the filter applies automatically to all queries. For background functions that need to process **all** records (including inactive), call `.IgnoreQueryFilters()`:

```csharp
var allProducts = await _dbContext.Products
    .IgnoreQueryFilters()
    .ToListAsync();
```

For seasonal discount logic, active-only is correct — no override needed.

---

## Gotchas

- **`DbContext` is scoped** — this is fine for queue/timer triggers since each invocation is a new scope.
- **Don't use `FindAsync` for int PKs on tables with composite keys** — use `FirstOrDefaultAsync` with a predicate instead.
- **`local.settings.json` is gitignored** — never commit connection strings. Production connection strings go in Azure App Configuration / Key Vault references.
- **Solution file**: this project uses `.slnx` format (`OutdoorsShop.slnx`), not `.sln`. Use `dotnet build OutdoorsShop.slnx` from repo root.
- **EF migrations**: run from repo root with explicit `--project` and `--startup-project` flags pointing to Infrastructure and Api respectively.
