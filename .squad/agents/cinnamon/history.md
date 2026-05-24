# history — History (summary)

- Full history archived to history-archive-20260524T145749Z.md

- Recent highlights:
- # Cinnamon — History
- 
- ## Core Context
- 
- - **Project:** Outdoors Shop
- - **Owner:** Jorgito
- - **Role:** Backend Developer
- - **Joined:** 2026-05-23
- - **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- - **Stack:** .NET 10 Web API (C#) | ASP.NET Core | EF Core | Azure SQL Database | Azure Functions (.NET isolated) | Azure Blob Storage | JWT auth
- - **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, OrderItems, Inventory
- - **My scope:** .NET 10 Web API, EF Core + Azure SQL, Azure Functions (seasonal discounts/payment confirmation/stock updates), Azure Blob Storage (product images/receipts/exports), JWT auth backend, CSV/Excel report generation
- - **Team:** Toru (Architect), Malta (Frontend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- - **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development
- 
- ## Learnings

### 2026-05-24T19:19:19.460-03:00 — Live bug fix: CORS origin mismatch

- **Both bugs (registration + catalog "Failed to fetch") had ONE root cause**: `AllowedOrigins__0` in the App Service env var pointed to `brave-beach-044d7c610.6.azurestaticapps.net`, but the actual live SWA is `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`. The `wonderful-plant` SWA is the one in `rg-outdoors-dev` with the React app deployed; `brave-beach` returns an Azure 404.
- **Diagnostic pattern**: CORS preflight returning 204 with NO `Access-Control-Allow-Origin` header = origin rejected by CORS policy. vs 204 WITH `Access-Control-Allow-Origin` = origin accepted.
- **How to identify the live SWA**: Use `az staticwebapp list --query "[].{name:name,url:defaultHostname}"` to see all SWAs and their URLs. Cross-check against CORS config.
- **How to identify what's deployed on a SWA**: Fetch the root HTML, extract the JS bundle URL, then grep the bundle for `app-outdoors` or `localhost` to confirm which API URL is baked in.
- **App Service env vars override appsettings.json**: Updating `AllowedOrigins__0` in the App Service config takes effect within ~30 seconds (no redeploy needed). Still update appsettings.json + commit so code matches reality.
- **Fix**: Updated `AllowedOrigins__0` App Service env var + `appsettings.json`, committed as `68c2509` to dev.
- **Verified**: Both `OPTIONS /api/v1/products` and `OPTIONS /api/v1/auth/register` from `wonderful-plant` origin now return 204 with proper ACAO headers.

### 2026-05-24T18:57:46.744-03:00 — dev → main merge

- `main` branch is checked out in the `.copilot-main` linked worktree; `git checkout main` from the repo root is blocked. All main-branch operations must run from `.copilot-main`.
- Merge conflict in `Program.cs` arose because main had the basic role-seeding block while dev had the extended version with `ILogger<Program>` and admin user seed. Resolution: always prefer the dev (fuller) version.
- Merge commit: `56f6dec` — "Merge dev into main: auth fixes, CORS, Swagger, blob image upload, admin seed". Push to `origin/main` succeeded.
- Commits synced: `22e971e` (cookie/JWT fix), `cada3b2` (CORS hardening), `9076954`+`943db2e` (Swagger all envs), `708af75` (admin seed), `526b8fa` (blob upload endpoint), `164a8e7` (frontend upload UI), plus squad/docs commits.
- 
- ### 2026-05-24T16:52:12.609-03:00 — Admin user seed
- 
- - Added idempotent admin user seed to `Program.cs` after role seeding — uses `FindByEmailAsync` guard to prevent duplicates.
- - Admin user: `admin@outdoorsshop.dev` / `Admin@123456`, role `Administrator`, Customer.Name `"Admin User"`.
- - `given_name` claim in JWT comes from `Customer.Name`; must create Customer record alongside the Identity user or the claim will fall back to UserName (which is email).
- - **Oryx double `.runtimeconfig.json` trap:** If a previous `publish_output` folder exists inside the project directory, dotnet publish will include it in the new output zip, causing Oryx to find 2 `.runtimeconfig.json` files and fall back to `hostingstart.dll`. Fix: set `WEBSITE_STARTUP_FILE = dotnet OutdoorsShop.Api.dll` via `az webapp config set --startup-file`.
- - Commit: `708af75`; deployed to `app-outdoors-api-dev`; login smoke test confirmed 200 + JWT with `given_name: "Admin User"` and role `Administrator`.
- 
- ### 2026-05-24T16:52:12.609-03:00 — Azure Blob Storage product image upload
- 
- - Most blob infrastructure was already in place (interface, service, NuGet, config placeholder, DI wiring); only the upload method, endpoint, and Azure container setup were missing.
- - Added `UploadProductImageAsync(Stream, string, string, int)` to `IBlobStorageService` — no `IFormFile` in Core to keep ASP.NET out of the domain layer; controller handles `IFormFile` extraction.
- - `BlobStorageService.UploadProductImageAsync` creates `product-images` container with `PublicAccessType.Blob`; blob name: `products/{productId}/{guid}{ext}`.
- - `[Consumes("multipart/form-data")]` causes 415 before auth runs — always include correct content type when testing auth.
- - Real connection string injected via `AzureStorage__ConnectionString` App Service env var; placeholder remains in appsettings.json.
- - Commit: `526b8fa`; deployed to `app-outdoors-api-dev`; 401/403/health smoke tests confirmed.
- 
- ### 2026-05-24T16:24:29.079-03:00 — Swagger enabled in all environments
- 
- - Switched the API from development-only OpenAPI mapping to Swashbuckle middleware so `/swagger` and `/swagger/v1/swagger.json` stay available in every environment.
- - Build validation: `dotnet build` succeeded from `src/OutdoorsShop.Api` after adding `Swashbuckle.AspNetCore` and wiring `UseSwagger()` / `UseSwaggerUI()`.
- - Deployment validation: published linux-x64, zipped with `System.IO.Compression.ZipFile`, uploaded `webapp-releases/api-dev.zip`, refreshed `WEBSITE_RUN_FROM_PACKAGE`, restarted `app-outdoors-api-dev`, and verified both Swagger endpoints returned `200` in production.

## 2026-05-24 — Swagger in Production (cinnamon-6)
- Removed IsDevelopment() guard from Swagger/SwaggerUI in Program.cs
- Enabled Swagger in ALL environments (dev, staging, production)
- Deployed commits 9076954 + 943db2e to dev
- Verified: /swagger and /swagger/v1/swagger.json → 200 on app-outdoors-api-dev.azurewebsites.net
- IMPORTANT: Correct API hostname is app-outdoors-api-dev.azurewebsites.net (NOT outdoors-shop-api-dev)
- Backlog item #1 completed
- 
- ### 2026-05-24T15:30:00-03:00 — CORS fix: SWA URL + platform CORS trap

## 2026-05-24 — CORS Fix (cinnamon-5)
- Fixed CORS AllowedOrigins: added SWA URL brave-beach-044d7c610.6.azurestaticapps.net
- Removed stale blob storage origin stoutdoorswebdev.z1.web.core.windows.net
- Cleared rogue Azure platform CORS entry wonderful-plant-0a1ca5f0f.7.azurestaticapps.net that was overriding ASP.NET Core CORS entirely
- Hardened Program.cs to use GetChildren() for reliable env-var array reading
- Deployed commit cada3b2 to dev — smoke test confirmed ACAO header on health, preflight, products
- Creta independently verified: 8/8 cross-origin tests passed
- 
- - **Root cause was Azure platform CORS, not code:** `WEBSITE_CORS_ALLOWED_ORIGINS` was set to a stale SWA URL (`wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`), which caused Azure App Service to intercept and reject all CORS preflight requests before ASP.NET Core middleware ran. The fix was `az webapp cors remove` to clear it. Platform CORS must remain empty — CORS is owned entirely by ASP.NET Core middleware.
- - **AllowedOrigins config reading:** `GetSection("AllowedOrigins").Get<string[]>()` can silently return an empty array when env vars and appsettings.json both define the same indexed keys — use `GetChildren().Select(c => c.Value ?? "").Where(v => v.Length > 0).ToArray()` instead.
- - **WEBSITE_RUN_FROM_PACKAGE caching:** When the blob URL is unchanged, Azure App Service may serve a cached local zip from `/home/data/SitePackages/`. Force a fresh pickup by either (a) uploading to a new blob name and updating the SAS URL, or (b) uploading directly to `/home/data/SitePackages/` via Kudu VFS and updating `packagename.txt`.
- - **SAS URL via az CLI in PowerShell:** `az storage blob generate-sas --full-uri` has ampersands in the URL that break as command separators. Build the URL manually: `$sas = az storage blob generate-sas ... -o tsv; $url = "https://...blob.../$name?$sas"`. Use a JSON file (`@file.json`) when passing the SAS URL to `az webapp config appsettings set`.
- - **Deployment pattern confirmed:** Publish linux-x64 zip → upload to `stoutdoorsdev/webapp-releases/` → update `WEBSITE_RUN_FROM_PACKAGE` SAS URL → app auto-restarts and picks up new code.
- - **Smoke test:** After deploy, confirmed `Access-Control-Allow-Origin: https://brave-beach-044d7c610.6.azurestaticapps.net` returned for GET `/api/health`, GET `/api/v1/products`, and OPTIONS preflight on `/api/v1/products`.
- 
- ### 2026-05-24T15:11:02.555-03:00 — Auth refresh cookie cross-origin fix
- 
- - **Refresh cookie policy:** `src/OutdoorsShop.Api/Controllers/AuthController.cs` must use `SameSite=None` with `Secure=true` for both the refresh-token set cookie and the logout clear-cookie path; `SameSite=Strict` breaks cross-origin refresh when the frontend origin differs from the API origin.
- - **JWT display name claim:** `GenerateTokenAsync` should populate `given_name` from `customer.Name` instead of `user.UserName`, because registration stores the email in `UserName`.
- - **Deployment/verification:** Published `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj` for Linux, zipped via `ZipFile.CreateFromDirectory`, uploaded to `stoutdoorsdev/webapp-releases/api-dev.zip`, restarted `app-outdoors-api-dev`, then verified `POST /api/v1/auth/register` and `POST /api/v1/auth/refresh` both returned `200` and the live `Set-Cookie` header now shows `samesite=none`.
- 
- ### 2026-05-23 — EF Core migration + entity key convention fix
- 
- - **Migration file location:** `src/OutdoorsShop.Infrastructure/Data/Migrations/20260523162304_InitialCreate.cs`

- **Migration file location:** `src/OutdoorsShop.Infrastructure/Data/Migrations/20260523162304_InitialCreate.cs`
- **AppDbContextFactory:** already implements `IDesignTimeDbContextFactory<AppDbContext>` and reads env vars — no changes needed
- **Key fix required:** EF Core convention only auto-detects PKs named `Id` or `{ClassName}Id`. The entities used non-matching names (`CategoryID` for `ProductCategory`, `OrderID` for `SalesOrder`, `OrderDetailID` for `SalesOrderDetail`). Fixed by adding `HasKey()` in `AppDbContext.OnModelCreating`.
- **DB permission blocker:** `ShopAdmin` user lacks `CREATE TABLE` (DDL) permission on `OutdoorsShopDB`. Needs server admin to run `ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;` on Azure SQL before `database update` will succeed.
- **User Secrets ID:** `749208c0-6506-4fba-ac59-228ef8899ee4` (stored in OutdoorsShop.Api.csproj)
- **Security:** Connection string in User Secrets only — never in committed files. `appsettings.Development.json` has `"USE_USER_SECRETS_OR_ENV_VAR"` placeholder.

### 2026-05-23 — Products and Categories CRUD

- **AppDbContext already had** category seeding and global query filters (`HasQueryFilter`) — no migration needed.
- **ProductRepository overrides** `GetByIdAsync` and `GetAllAsync` from the base `Repository<T>` to add `.Include(p => p.Category)`. All query methods include this.
- **ProductsController** injects `ICategoryRepository` in addition to `IProductRepository` and `IInventoryRepository` — validated category existence before Create/Update.
- **POST /products** automatically creates a `ProductInventory` record (qty=0, threshold=5) in the same request.
- **Soft delete** sets `IsActive = false` and calls `UpdateAsync` — global query filter then hides the record automatically.
- **CategoryDto** placed in `src/OutdoorsShop.Core/DTOs/Products/` alongside other product DTOs.

### 2026-05-23 — Auth endpoints
- AuthController was pre-scaffolded with register/login/refresh; added Logout and GET /me
- Refresh token stored as hash in AspNetUserTokens table (provider=OutdoorsShop, name=RefreshTokenHash)
- `.Result` inside LINQ on Identity Users = deadlock risk; fix is ToList() + async foreach
- AsAsyncEnumerable() requires EF Core using directive — not available in API layer without adding EF Core dep; ToList() is the safe alternative
- Logout: removes token hash from AspNetUserTokens + expires cookie with past date
- UserProfileDto lives in Core.DTOs.Auth

### 2026-05-23T19:36:12.645-03:00 — Azure Functions implementation

- **Entity IDs are int, not Guid**: `Product.ProductID`, `SalesOrder.OrderID`, `ProductInventory.ProductID` are all `int`. Queue message contracts were adapted to use `int` for entity lookups, not Guid.
- **OrderStatus is an enum**: `SalesOrder.Status` is `OrderStatus` enum (Pending/Processing/Shipped/Delivered/Cancelled), stored as string via `.HasConversion<string>()`. Payment confirmation maps "Success" → `OrderStatus.Processing`, "Failed" → `OrderStatus.Cancelled`.
- **PaymentStatus enum**: Pending/Confirmed/Failed — "Success" in queue message maps to `PaymentStatus.Confirmed`.
- **SeasonalDiscount schedule**: `0 0 2 * * *` (02:00 UTC daily). Season detection by UTC month; Winter (Dec/Jan/Feb) → Camping+Trekking 15% off; Summer (Jun/Jul/Aug) → Cycling+Climbing 10% off; Spring/Autumn → reset to 1.0. Global query filter for IsActive applies automatically.
- **PaymentConfirmation queue**: `payment-confirmations`. On Failed: loads `order.Details` eagerly and restores `ProductInventory.QuantityAvailable` for each line item.
- **StockUpdate**: creates `ProductInventory` record if missing (default threshold=5). Logs `StockUpdateLog` (Guid PK, int ProductId). Quantity clamped to ≥0.
- **Migrations added**: `AddProductDiscountMultiplier`, `AddOrderPaymentFields`, `AddStockUpdateLog`.
- **New entity fields**: `Product.DiscountMultiplier` (decimal, default 1.0, precision 5,4); `SalesOrder.PaymentReference` (string?), `SalesOrder.PaidAt` (DateTimeOffset?).
- **DI**: Functions host already had AppDbContext + all repositories registered in Program.cs — no changes needed.
- **Solution file**: `OutdoorsShop.slnx` (not `.sln`) at repo root.

- **Service layer added for protected business rules:** `CustomerService`, `OrderService`, and `InventoryService` live in `src/OutdoorsShop.Infrastructure/Services/` and are wired in `src/OutdoorsShop.Api/Extensions/ServiceCollectionExtensions.cs` via `AddDomainServices()`.
- **Pagination contract:** shared `PagedResult<T>` lives in `src/OutdoorsShop.Core/DTOs/Common/`; Customers, Orders, and Inventory list endpoints now return paged payloads instead of raw collections.
- **Customer ownership check:** controllers read JWT `customer_id`, but the allow/deny decision happens inside `ICustomerService` / `IOrderService`; controllers only translate service results to HTTP responses.
- **Order creation path:** `OrderService.CreateAsync` validates active products, checks inventory, enforces current catalog pricing against submitted `UnitPrice`, creates `SalesOrder` + `SalesOrderDetail`, and decrements stock inside one EF Core transaction.
- **Report export pattern:** `ReportsController` gets row DTOs from services and handles file formatting with `CsvHelper` + `ClosedXML`; file generation stays in API, data shaping stays in services.
- **Key file paths:** `src/OutdoorsShop.Api/Controllers/CustomersController.cs`, `src/OutdoorsShop.Api/Controllers/OrdersController.cs`, `src/OutdoorsShop.Api/Controllers/InventoryController.cs`, `src/OutdoorsShop.Api/Controllers/ReportsController.cs`, `src/OutdoorsShop.Infrastructure/Services/OrderService.cs`.

### 2026-05-23T20:39:55.398-03:00 — GitHub Actions CI/CD workflows

- **Three workflows created** in `.github/workflows/`: `backend.yml`, `frontend.yml`, `functions.yml`.
- **Solution file is `OutdoorsShop.slnx`** (not `.sln`) at repo root — dotnet CLI commands reference this path, not `src/OutdoorsShop.sln`.
- **backend.yml**: triggers on push/PR to `main`/`dev` for `src/**`; restores, builds, and tests the full solution; uploads `.trx` results as artifact; writes passed/failed/skipped table to job summary.
- **frontend.yml**: triggers on push/PR to `main`/`dev` for `frontend/**`; runs `npm ci` + `npm run build`; uploads `frontend/dist` as artifact. Uses npm cache keyed on `frontend/package-lock.json`.
- **functions.yml**: triggers on push/PR to `main`/`dev` for `src/OutdoorsShop.Functions/**`; builds only the Functions project; runs all tests in `OutdoorsShop.Tests`; publishes Functions artifact to `publish/functions` with a placeholder comment for Azure deploy step.
- **All workflows**: `permissions: contents: read`, `actions/checkout@v4`, `actions/setup-dotnet@v4` (`10.x`), `actions/setup-node@v4` (`20`), `concurrency` groups with `cancel-in-progress: true`, badge comment at top of each file.

### 2026-05-24T00:58:00.000-03:00 — Product & Inventory seed

- **Script location:** `scripts/seed-products.sql` (committed to repo root `/scripts/`)
- **16 products seeded:** 4 per category (Camping/Trekking/Cycling/Climbing), all `IsActive=1`, `DiscountMultiplier=1.0`.
- **Inventory column names differ from task spec:** actual DB columns are `QuantityAvailable` (not `QuantityOnHand`) and `ReorderThreshold` (not `ReorderLevel`). Always introspect `INFORMATION_SCHEMA.COLUMNS` before inserting into Inventory.
- **IDENTITY_INSERT required:** Products table has an IDENTITY PK — `SET IDENTITY_INSERT Products ON/OFF` is needed when seeding with explicit IDs.
- **Guard clause:** script opens with `IF EXISTS (SELECT 1 FROM Products WHERE IsActive=1) RETURN` to be idempotent.
- **Verified via API:** `GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products after seed.
- **Credentials path:** ShopAdmin password lives in user secrets (`749208c0-6506-4fba-ac59-228ef8899ee4`) — never committed.

### 2026-05-23T21:00:31.176-03:00 — TimeProvider injection for date-dependent Azure Functions testing

- **Pattern used:** .NET 8+ `System.TimeProvider` abstract class — the canonical Microsoft abstraction for time. No custom `ITimeProvider` interface needed.
- **SeasonalDiscountFunction** now accepts `TimeProvider? timeProvider = null` as an optional constructor parameter; defaults to `TimeProvider.System` so production DI wiring is backward-compatible.
- **`_timeProvider.GetUtcNow().UtcDateTime`** replaces all `DateTime.UtcNow` calls in the function.
- **FakeTimeProvider** pattern: a `sealed class FakeTimeProvider : TimeProvider` that overrides `GetUtcNow()` returning a pinned `DateTimeOffset` — lets each test fix the date to a specific month.
- **`builder.Services.AddSingleton(TimeProvider.System);`** added to `src/OutdoorsShop.Functions/Program.cs` so DI injects the real clock in production.
- **Result:** 4 previously skipped tests now pass; total functions test count: 20 passed, 0 skipped.

### 2026-05-24T12:58:17.459-03:00 — Azure Functions 503 root cause and Flex fix

- **Root cause:** `func-outdoors-dev` was deployed as `.NET 10` isolated on the classic **Linux Consumption (Y1)** plan. Microsoft documents that `.NET 10` isolated apps can't run on Linux Consumption; they must use **Flex Consumption** instead.
- **Observed symptoms:** `https://func-outdoors-dev.azurewebsites.net` and `/api/health` returned `503`, `az functionapp function list` failed with host `ServiceUnavailable`, and the original workflow had no deploy step.
- **Azure fix applied:** Deleted the broken Linux Consumption app, recreated `func-outdoors-dev` on **Flex Consumption** in `westus3`, re-granted Key Vault secret access to the new managed identity, restored the app settings, and redeployed the Functions package.
- **Deployment packaging detail:** Flex zip deploy requires the `.azurefunctions/` directory at the zip root. Packaging from the publish directory root (for example `tar -a -cf <zip> .`) works; wildcard-only archives can drop hidden folders and fail validation.
- **Repo follow-up:** Added an anonymous `GET /api/health` function, updated `functions.yml` to publish a Linux package and deploy it with Azure CLI, and updated infra/docs toward Flex Consumption.
- **Verification:** `az functionapp function list` now returns Health + the three background functions, `GET https://func-outdoors-dev.azurewebsites.net/api/health` returns `200 {"status":"ok"}`, and the site root returns `200`.

### 2026-05-24T13:49:18.068-03:00 — Queue trigger recovery on Flex Consumption

- **Storage account:** `AzureWebJobsStorage` for `func-outdoors-dev` resolves to storage account `stoutdoorsdev`.
- **Queue provisioning:** created the missing `payment-confirmations` and `stock-updates` queues in `stoutdoorsdev`.
- **Code contract check:** `src/OutdoorsShop.Functions/Functions/PaymentConfirmationFunction.cs` and `src/OutdoorsShop.Functions/Functions/StockUpdateFunction.cs` already bind to `payment-confirmations` and `stock-updates` exactly — no source code rename was needed.
- **Flex runtime fix:** queue messages still sat with `dequeueCount = 0` after queue creation, restart, and trigger sync. Adding function-specific always-ready instances (`function:PaymentConfirmation=1`, `function:StockUpdate=1`) in the Flex scale config brought the queue listeners online.
- **Verification:** after the always-ready change and restart, both queues drained their smoke-test messages; `stock-updates` cleared within ~30 seconds and `payment-confirmations` cleared on the next check after ~90 seconds.
- **Diagnostics:** Application Insights showed the function group targets for `function:paymentconfirmation` and `function:stockupdate` after the scale update, and no new queue-function exceptions were recorded during verification.

### 2026-05-24T14:43:10.624-03:00 — Identity role seeding + /api/health fix

- **Root cause:** `AspNetRoles` table was empty on cold start; `AddToRoleAsync("Customer")` fails with 500 if the role row doesn't exist.
- **Fix:** Added a startup role-seeding block in `src/OutdoorsShop.Api/Program.cs` immediately before `app.Run()`. Uses `RoleManager<IdentityRole>` (already registered via `AddIdentity<ApplicationUser, IdentityRole>`) to idempotently create `Administrator` and `Customer` roles.
- **Health endpoint:** Added `app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous()` as a minimal-API endpoint (no controller needed).
- **Deploy pattern reminder:** `WEBSITE_RUN_FROM_PACKAGE` uses a blob SAS URL. Must publish for Linux (`-r linux-x64 --self-contained false /p:UseAppHost=false`), zip the publish output using `[System.IO.Compression.ZipFile]::CreateFromDirectory` (not `Compress-Archive -Path *` — wildcard zip only captured 3 entries), upload to `stoutdoorsdev/webapp-releases/api-dev.zip`, then restart the App Service.
- **Verification:** `GET /api/health` → `200 {"status":"ok"}`; `POST /api/v1/auth/register` → `200` with JWT; `POST /api/v1/auth/login` → `200` with JWT. No more 500 on registration.
- **Committed:** `dev` (786cc88) and cherry-picked to `main` (a6a1780) via `.copilot-main` worktree. Both branches pushed.

### 2026-05-24T14:24:58.550-03:00 — Product image URLs wired for all 16 products

- **Approach:** Used Unsplash CDN free-tier images (no attribution required for display). Each product got a unique, relevant photo URL in the format `https://images.unsplash.com/photo-{id}?w=400&fit=crop&auto=format`.
- **Image assignment:** Camping (IDs 1–4): campfire/tent, sleeping bag, camp stove, night hiking; Trekking (IDs 5–8): hiking trail, hiking boots, hydration pack, GPS/map; Cycling (IDs 9–12): mountain biking, cycling, bike lights, bike repair; Climbing (IDs 13–16): sport climbing, bouldering/chalk, climbing shoes, carabiners.
- **SQL approach:** Created `scripts/update-image-urls.sql` and ran it via `sqlcmd` against `azure-sql-pampa.database.windows.net / OutdoorsShopDB` using `ShopAdmin` credentials (from user secrets). Required opening a firewall rule for agent IP first: `az sql server firewall-rule create --resource-group AzureSqlRg --server azure-sql-pampa --name AllowCinnamonAgent`.
- **SQL server lives in `AzureSqlRg`** (not `rg-outdoors-dev`) — the server predates the project's resource group.
- **Seed script updated:** `scripts/seed-products.sql` now includes the Unsplash URLs in the INSERT values (replacing NULL) so future reseeds will also populate ImageUrl.
- **Verification:** `GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products, all with non-null `imageUrl` fields.
