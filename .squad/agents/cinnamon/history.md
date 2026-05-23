# Cinnamon — History

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

### 2026-05-23 — EF Core migration + entity key convention fix

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
