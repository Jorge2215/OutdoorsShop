# Cinnamon â€” History

## Core Context

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Role:** Backend Developer
- **Joined:** 2026-05-23
- **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- **Stack:** .NET 10 Web API (C#) | ASP.NET Core | EF Core | Azure SQL Database | Azure Functions (.NET isolated) | Azure Blob Storage | JWT auth
- **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, OrderItems, Inventory
- **My scope:** .NET 10 Web API, EF Core + Azure SQL, Azure Functions (seasonal discounts/payment confirmation/stock updates), Azure Blob Storage (product images/receipts/exports), JWT auth backend, CSV/Excel report generation
- **Team:** Toru (Architect), Malta (Frontend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development

## Learnings

### 2026-05-23 â€” EF Core migration + entity key convention fix

- **Migration file location:** `src/OutdoorsShop.Infrastructure/Data/Migrations/20260523162304_InitialCreate.cs`
- **AppDbContextFactory:** already implements `IDesignTimeDbContextFactory<AppDbContext>` and reads env vars â€” no changes needed
- **Key fix required:** EF Core convention only auto-detects PKs named `Id` or `{ClassName}Id`. The entities used non-matching names (`CategoryID` for `ProductCategory`, `OrderID` for `SalesOrder`, `OrderDetailID` for `SalesOrderDetail`). Fixed by adding `HasKey()` in `AppDbContext.OnModelCreating`.
- **DB permission blocker:** `ShopAdmin` user lacks `CREATE TABLE` (DDL) permission on `OutdoorsShopDB`. Needs server admin to run `ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;` on Azure SQL before `database update` will succeed.
- **User Secrets ID:** `749208c0-6506-4fba-ac59-228ef8899ee4` (stored in OutdoorsShop.Api.csproj)
- **Security:** Connection string in User Secrets only â€” never in committed files. `appsettings.Development.json` has `"USE_USER_SECRETS_OR_ENV_VAR"` placeholder.

### 2026-05-23 â€” Products and Categories CRUD

- **AppDbContext already had** category seeding and global query filters (`HasQueryFilter`) â€” no migration needed.
- **ProductRepository overrides** `GetByIdAsync` and `GetAllAsync` from the base `Repository<T>` to add `.Include(p => p.Category)`. All query methods include this.
- **ProductsController** injects `ICategoryRepository` in addition to `IProductRepository` and `IInventoryRepository` â€” validated category existence before Create/Update.
- **POST /products** automatically creates a `ProductInventory` record (qty=0, threshold=5) in the same request.
- **Soft delete** sets `IsActive = false` and calls `UpdateAsync` â€” global query filter then hides the record automatically.
- **CategoryDto** placed in `src/OutdoorsShop.Core/DTOs/Products/` alongside other product DTOs.

### 2026-05-23 â€” Auth endpoints
- AuthController was pre-scaffolded with register/login/refresh; added Logout and GET /me
- Refresh token stored as hash in AspNetUserTokens table (provider=OutdoorsShop, name=RefreshTokenHash)
- `.Result` inside LINQ on Identity Users = deadlock risk; fix is ToList() + async foreach
- AsAsyncEnumerable() requires EF Core using directive â€” not available in API layer without adding EF Core dep; ToList() is the safe alternative
- Logout: removes token hash from AspNetUserTokens + expires cookie with past date
- UserProfileDto lives in Core.DTOs.Auth

### 2026-05-23T19:36:12.645-03:00 â€” Azure Functions implementation

- **Entity IDs are int, not Guid**: `Product.ProductID`, `SalesOrder.OrderID`, `ProductInventory.ProductID` are all `int`. Queue message contracts were adapted to use `int` for entity lookups, not Guid.
- **OrderStatus is an enum**: `SalesOrder.Status` is `OrderStatus` enum (Pending/Processing/Shipped/Delivered/Cancelled), stored as string via `.HasConversion<string>()`. Payment confirmation maps "Success" â†’ `OrderStatus.Processing`, "Failed" â†’ `OrderStatus.Cancelled`.
- **PaymentStatus enum**: Pending/Confirmed/Failed â€” "Success" in queue message maps to `PaymentStatus.Confirmed`.
- **SeasonalDiscount schedule**: `0 0 2 * * *` (02:00 UTC daily). Season detection by UTC month; Winter (Dec/Jan/Feb) â†’ Camping+Trekking 15% off; Summer (Jun/Jul/Aug) â†’ Cycling+Climbing 10% off; Spring/Autumn â†’ reset to 1.0. Global query filter for IsActive applies automatically.
- **PaymentConfirmation queue**: `payment-confirmations`. On Failed: loads `order.Details` eagerly and restores `ProductInventory.QuantityAvailable` for each line item.
- **StockUpdate**: creates `ProductInventory` record if missing (default threshold=5). Logs `StockUpdateLog` (Guid PK, int ProductId). Quantity clamped to â‰¥0.
- **Migrations added**: `AddProductDiscountMultiplier`, `AddOrderPaymentFields`, `AddStockUpdateLog`.
- **New entity fields**: `Product.DiscountMultiplier` (decimal, default 1.0, precision 5,4); `SalesOrder.PaymentReference` (string?), `SalesOrder.PaidAt` (DateTimeOffset?).
- **DI**: Functions host already had AppDbContext + all repositories registered in Program.cs â€” no changes needed.
- **Solution file**: `OutdoorsShop.slnx` (not `.sln`) at repo root.

- **Service layer added for protected business rules:** `CustomerService`, `OrderService`, and `InventoryService` live in `src/OutdoorsShop.Infrastructure/Services/` and are wired in `src/OutdoorsShop.Api/Extensions/ServiceCollectionExtensions.cs` via `AddDomainServices()`.
- **Pagination contract:** shared `PagedResult<T>` lives in `src/OutdoorsShop.Core/DTOs/Common/`; Customers, Orders, and Inventory list endpoints now return paged payloads instead of raw collections.
- **Customer ownership check:** controllers read JWT `customer_id`, but the allow/deny decision happens inside `ICustomerService` / `IOrderService`; controllers only translate service results to HTTP responses.
- **Order creation path:** `OrderService.CreateAsync` validates active products, checks inventory, enforces current catalog pricing against submitted `UnitPrice`, creates `SalesOrder` + `SalesOrderDetail`, and decrements stock inside one EF Core transaction.
- **Report export pattern:** `ReportsController` gets row DTOs from services and handles file formatting with `CsvHelper` + `ClosedXML`; file generation stays in API, data shaping stays in services.
- **Key file paths:** `src/OutdoorsShop.Api/Controllers/CustomersController.cs`, `src/OutdoorsShop.Api/Controllers/OrdersController.cs`, `src/OutdoorsShop.Api/Controllers/InventoryController.cs`, `src/OutdoorsShop.Api/Controllers/ReportsController.cs`, `src/OutdoorsShop.Infrastructure/Services/OrderService.cs`.

### 2026-05-23T20:39:55.398-03:00 â€” GitHub Actions CI/CD workflows

- **Three workflows created** in `.github/workflows/`: `backend.yml`, `frontend.yml`, `functions.yml`.
- **Solution file is `OutdoorsShop.slnx`** (not `.sln`) at repo root â€” dotnet CLI commands reference this path, not `src/OutdoorsShop.sln`.
- **backend.yml**: triggers on push/PR to `main`/`dev` for `src/**`; restores, builds, and tests the full solution; uploads `.trx` results as artifact; writes passed/failed/skipped table to job summary.
- **frontend.yml**: triggers on push/PR to `main`/`dev` for `frontend/**`; runs `npm ci` + `npm run build`; uploads `frontend/dist` as artifact. Uses npm cache keyed on `frontend/package-lock.json`.
- **functions.yml**: triggers on push/PR to `main`/`dev` for `src/OutdoorsShop.Functions/**`; builds only the Functions project; runs all tests in `OutdoorsShop.Tests`; publishes Functions artifact to `publish/functions` with a placeholder comment for Azure deploy step.
- **All workflows**: `permissions: contents: read`, `actions/checkout@v4`, `actions/setup-dotnet@v4` (`10.x`), `actions/setup-node@v4` (`20`), `concurrency` groups with `cancel-in-progress: true`, badge comment at top of each file.

### 2026-05-24T00:58:00.000-03:00 â€” Product & Inventory seed

- **Script location:** `scripts/seed-products.sql` (committed to repo root `/scripts/`)
- **16 products seeded:** 4 per category (Camping/Trekking/Cycling/Climbing), all `IsActive=1`, `DiscountMultiplier=1.0`.
- **Inventory column names differ from task spec:** actual DB columns are `QuantityAvailable` (not `QuantityOnHand`) and `ReorderThreshold` (not `ReorderLevel`). Always introspect `INFORMATION_SCHEMA.COLUMNS` before inserting into Inventory.
- **IDENTITY_INSERT required:** Products table has an IDENTITY PK â€” `SET IDENTITY_INSERT Products ON/OFF` is needed when seeding with explicit IDs.
- **Guard clause:** script opens with `IF EXISTS (SELECT 1 FROM Products WHERE IsActive=1) RETURN` to be idempotent.
- **Verified via API:** `GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products after seed.
- **Credentials path:** ShopAdmin password lives in user secrets (`749208c0-6506-4fba-ac59-228ef8899ee4`) â€” never committed.

### 2026-05-23T21:00:31.176-03:00 â€” TimeProvider injection for date-dependent Azure Functions testing

- **Pattern used:** .NET 8+ `System.TimeProvider` abstract class â€” the canonical Microsoft abstraction for time. No custom `ITimeProvider` interface needed.
- **SeasonalDiscountFunction** now accepts `TimeProvider? timeProvider = null` as an optional constructor parameter; defaults to `TimeProvider.System` so production DI wiring is backward-compatible.
- **`_timeProvider.GetUtcNow().UtcDateTime`** replaces all `DateTime.UtcNow` calls in the function.
- **FakeTimeProvider** pattern: a `sealed class FakeTimeProvider : TimeProvider` that overrides `GetUtcNow()` returning a pinned `DateTimeOffset` â€” lets each test fix the date to a specific month.
- **`builder.Services.AddSingleton(TimeProvider.System);`** added to `src/OutdoorsShop.Functions/Program.cs` so DI injects the real clock in production.
- **Result:** 4 previously skipped tests now pass; total functions test count: 20 passed, 0 skipped.

### 2026-05-24T12:58:17.459-03:00 â€” Azure Functions 503 root cause and Flex fix

- **Root cause:** `func-outdoors-dev` was deployed as `.NET 10` isolated on the classic **Linux Consumption (Y1)** plan. Microsoft documents that `.NET 10` isolated apps can't run on Linux Consumption; they must use **Flex Consumption** instead.
- **Observed symptoms:** `https://func-outdoors-dev.azurewebsites.net` and `/api/health` returned `503`, `az functionapp function list` failed with host `ServiceUnavailable`, and the original workflow had no deploy step.
- **Azure fix applied:** Deleted the broken Linux Consumption app, recreated `func-outdoors-dev` on **Flex Consumption** in `westus3`, re-granted Key Vault secret access to the new managed identity, restored the app settings, and redeployed the Functions package.
- **Deployment packaging detail:** Flex zip deploy requires the `.azurefunctions/` directory at the zip root. Packaging from the publish directory root (for example `tar -a -cf <zip> .`) works; wildcard-only archives can drop hidden folders and fail validation.
- **Repo follow-up:** Added an anonymous `GET /api/health` function, updated `functions.yml` to publish a Linux package and deploy it with Azure CLI, and updated infra/docs toward Flex Consumption.
- **Verification:** `az functionapp function list` now returns Health + the three background functions, `GET https://func-outdoors-dev.azurewebsites.net/api/health` returns `200 {"status":"ok"}`, and the site root returns `200`.

### 2026-05-24T13:49:18.068-03:00 â€” Queue trigger recovery on Flex Consumption

- **Storage account:** `AzureWebJobsStorage` for `func-outdoors-dev` resolves to storage account `stoutdoorsdev`.
- **Queue provisioning:** created the missing `payment-confirmations` and `stock-updates` queues in `stoutdoorsdev`.
- **Code contract check:** `src/OutdoorsShop.Functions/Functions/PaymentConfirmationFunction.cs` and `src/OutdoorsShop.Functions/Functions/StockUpdateFunction.cs` already bind to `payment-confirmations` and `stock-updates` exactly â€” no source code rename was needed.
- **Flex runtime fix:** queue messages still sat with `dequeueCount = 0` after queue creation, restart, and trigger sync. Adding function-specific always-ready instances (`function:PaymentConfirmation=1`, `function:StockUpdate=1`) in the Flex scale config brought the queue listeners online.
- **Verification:** after the always-ready change and restart, both queues drained their smoke-test messages; `stock-updates` cleared within ~30 seconds and `payment-confirmations` cleared on the next check after ~90 seconds.
- **Diagnostics:** Application Insights showed the function group targets for `function:paymentconfirmation` and `function:stockupdate` after the scale update, and no new queue-function exceptions were recorded during verification.

### 2026-05-24T14:43:10.624-03:00 â€” Identity role seeding + /api/health fix

- **Root cause:** `AspNetRoles` table was empty on cold start; `AddToRoleAsync("Customer")` fails with 500 if the role row doesn't exist.
- **Fix:** Added a startup role-seeding block in `src/OutdoorsShop.Api/Program.cs` immediately before `app.Run()`. Uses `RoleManager<IdentityRole>` (already registered via `AddIdentity<ApplicationUser, IdentityRole>`) to idempotently create `Administrator` and `Customer` roles.
- **Health endpoint:** Added `app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous()` as a minimal-API endpoint (no controller needed).
- **Deploy pattern reminder:** `WEBSITE_RUN_FROM_PACKAGE` uses a blob SAS URL. Must publish for Linux (`-r linux-x64 --self-contained false /p:UseAppHost=false`), zip the publish output using `[System.IO.Compression.ZipFile]::CreateFromDirectory` (not `Compress-Archive -Path *` â€” wildcard zip only captured 3 entries), upload to `stoutdoorsdev/webapp-releases/api-dev.zip`, then restart the App Service.
- **Verification:** `GET /api/health` â†’ `200 {"status":"ok"}`; `POST /api/v1/auth/register` â†’ `200` with JWT; `POST /api/v1/auth/login` â†’ `200` with JWT. No more 500 on registration.
- **Committed:** `dev` (786cc88) and cherry-picked to `main` (a6a1780) via `.copilot-main` worktree. Both branches pushed.

### 2026-05-24T14:24:58.550-03:00 â€” Product image URLs wired for all 16 products

- **Approach:** Used Unsplash CDN free-tier images (no attribution required for display). Each product got a unique, relevant photo URL in the format `https://images.unsplash.com/photo-{id}?w=400&fit=crop&auto=format`.
- **Image assignment:** Camping (IDs 1â€“4): campfire/tent, sleeping bag, camp stove, night hiking; Trekking (IDs 5â€“8): hiking trail, hiking boots, hydration pack, GPS/map; Cycling (IDs 9â€“12): mountain biking, cycling, bike lights, bike repair; Climbing (IDs 13â€“16): sport climbing, bouldering/chalk, climbing shoes, carabiners.
- **SQL approach:** Created `scripts/update-image-urls.sql` and ran it via `sqlcmd` against `azure-sql-pampa.database.windows.net / OutdoorsShopDB` using `ShopAdmin` credentials (from user secrets). Required opening a firewall rule for agent IP first: `az sql server firewall-rule create --resource-group AzureSqlRg --server azure-sql-pampa --name AllowCinnamonAgent`.
- **SQL server lives in `AzureSqlRg`** (not `rg-outdoors-dev`) â€” the server predates the project's resource group.
- **Seed script updated:** `scripts/seed-products.sql` now includes the Unsplash URLs in the INSERT values (replacing NULL) so future reseeds will also populate ImageUrl.
- **Verification:** `GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products, all with non-null `imageUrl` fields.

- 2026-05-24 — Toru: Added comprehensive architecture document (docs/architecture.md) and enhanced Section 10 (resource relationships). Commits: 7607d07, 37ac131.

