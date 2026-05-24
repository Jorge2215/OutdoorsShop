# OutdoorsShop — Application Architecture

> **Last updated:** 2026-05-24  
> **Author:** Toru (Architect)  
> **Status:** Current

---

## 1. Executive Summary

OutdoorsShop is a full-stack e-commerce proof-of-concept for outdoor gear, built to benchmark the GitHub Copilot + Squad AI-assisted development workflow against traditional development practices. The application is composed of a React + TypeScript frontend hosted on Azure Static Web Apps, a .NET 10 Web API running on Azure App Service, Azure Functions (.NET isolated) for background processing, an Azure SQL Database managed via EF Core, and Azure Blob Storage for product images and queue-driven async workflows. The system demonstrates a complete production-grade architecture — authentication, role-based access, CI/CD pipelines, and Infrastructure-as-Code — built entirely with AI assistance.

---

## 2. System Overview Diagram

```
  ┌───────────────────────────────────────────────────────────────────┐
  │                        Browser (React SPA)                        │
  │               Vite + TypeScript + Zustand + React Query           │
  └──────────────────────────────┬────────────────────────────────────┘
                                 │  HTTPS
  ┌──────────────────────────────▼────────────────────────────────────┐
  │             Azure Static Web App (SWA)                            │
  │    wonderful-plant-0a1ca5f0f.7.azurestaticapps.net               │
  │    Region: westus3  │  RG: rg-outdoors-dev                        │
  │    SPA routing via staticwebapp.config.json (navigationFallback)  │
  └──────────────────────────────┬────────────────────────────────────┘
                                 │  HTTPS REST (VITE_API_URL)
  ┌──────────────────────────────▼────────────────────────────────────┐
  │           Azure App Service — .NET 10 Web API                     │
  │    app-outdoors-api-dev.azurewebsites.net                         │
  │    Region: westus3  │  RG: rg-outdoors-dev                        │
  │    ASP.NET Core Identity + JWT + EF Core 10                       │
  │    CORS: AllowedOrigins from app settings (no platform CORS)      │
  └──────────┬─────────────────────────────────┬──────────────────────┘
             │  EF Core / SQL                  │  Azure.Storage.Queues
  ┌──────────▼──────────────┐     ┌────────────▼──────────────────────┐
  │   Azure SQL Database    │     │        Azure Blob Storage          │
  │   azure-sql-pampa       │     │   stoutdoorsdev  (westus3)         │
  │   OutdoorsShopDB        │     │   • product-images (CDN/Unsplash)  │
  │   RG: AzureSqlRg        │     │   • webapp-releases/api-dev.zip    │
  └─────────────────────────┘     │   • Queues:                        │
             │                    │     - payment-confirmations         │
             │                    │     - stock-updates                 │
             │                    └──────────────┬─────────────────────┘
             │                                   │  Queue triggers
  ┌──────────▼───────────────────────────────────▼─────────────────────┐
  │           Azure Functions — .NET 8 Isolated                         │
  │    func-outdoors-dev.azurewebsites.net                              │
  │    Region: westus3  │  RG: rg-outdoors-dev                          │
  │    Flex Consumption plan                                             │
  │    • HealthFunction         (HTTP)                                   │
  │    • SeasonalDiscountFunction (Timer — daily 02:00 UTC)              │
  │    • PaymentConfirmationFunction (Queue: payment-confirmations)      │
  │    • StockUpdateFunction     (Queue: stock-updates)                  │
  └─────────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────────┐
  │                    GitHub Actions CI/CD                              │
  │    Jorge2215/OutdoorsShop  │  Branches: dev → main                  │
  │    • frontend.yml   → SWA deploy                                     │
  │    • backend.yml    → build + test (App Service deploy manual)       │
  │    • functions.yml  → build + test + zip deploy to Functions App     │
  │    OIDC federated credentials (no stored service principal secrets)  │
  └─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Frontend Architecture

### Technology Stack

| Concern | Library / Tool |
|---|---|
| Framework | React 18 + TypeScript |
| Build tool | Vite |
| Styling | Tailwind CSS |
| Client state | Zustand |
| Server state | React Query |
| Routing | React Router v6 |
| HTTP client | Typed fetch wrapper (`src/api/`) |
| Linting | ESLint |

### Key Pages & Routes

| Route | Component | Access |
|---|---|---|
| `/` | `HomePage` | Public |
| `/products` | `ProductsPage` | Public |
| `/products/:id` | `ProductDetailPage` | Public |
| `/login` | `LoginPage` | Public |
| `/register` | `RegisterPage` | Public |
| `/cart` | `CartPage` | Customer |
| `/checkout` | `CheckoutPage` | Customer |
| `/checkout/confirmation` | `OrderConfirmationPage` | Customer |
| `/orders` | `OrdersPage` | Authenticated |
| `/orders/:id` | `OrderDetailPage` | Authenticated |
| `/profile` | `ProfilePage` | Authenticated |
| `/admin` | `AdminDashboardPage` | Administrator |
| `/admin/products` | `AdminProductsPage` | Administrator |
| `/admin/inventory` | `AdminInventoryPage` | Administrator |
| `/admin/orders` | `AdminOrdersPage` | Administrator |

All routes use lazy loading via `React.lazy` + `Suspense`. Unknown routes redirect to `/`.

### Auth Flow

1. On app boot, `AppShell` calls `authStore.refreshToken()` using the HttpOnly refresh-token cookie.
2. If successful, `accessToken` (15-min JWT) is stored **in memory** in the Zustand `authStore` (not persisted to localStorage).
3. Subsequent API calls attach the in-memory `accessToken` as a `Bearer` header.
4. On 401 responses, the API client auto-retries via `/auth/refresh` before failing.
5. `ProtectedRoute` and `AdminRoute` gate pages by role from the Zustand store.

### Cart Architecture (ADR-004)

The cart is **client-only**: items are stored in Zustand (`cartStore`) with localStorage persistence. There is no `Cart` table in the database. On checkout, the cart contents are submitted as a single `POST /api/v1/orders` request.

### SPA Routing Config

`frontend/public/staticwebapp.config.json` configures `navigationFallback` → rewrites all unknown paths to `/index.html`, enabling client-side routing. Static assets (JS, CSS, SVG, PNG) are excluded from the rewrite.

### Deployment

- **Host:** Azure Static Web App (`app-outdoorsweb-swa`, westus3)
- **URL:** `https://wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`
- **Pipeline:** `frontend.yml` — `npm ci` → `npm run build` → `Azure/static-web-apps-deploy@v1`
- **Environment variable:** `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net` (set at build time)

---

## 4. Backend Architecture

### Technology Stack

| Concern | Library / Tool |
|---|---|
| Framework | ASP.NET Core .NET 10 |
| Language | C# |
| ORM | EF Core 10 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Validation | FluentValidation |
| Mapping | Mapster |
| API docs | OpenAPI (`/openapi/v1.json`), Swagger (`/swagger`) |
| Blob Storage | Azure.Storage.Blobs |

### Project Structure

```
src/
├── OutdoorsShop.Api/           # HTTP layer: controllers, middleware, extensions, JWT config
│   ├── Controllers/            # 7 controllers (see below)
│   ├── Extensions/             # DI registration helpers
│   ├── Middleware/             # ExceptionHandlingMiddleware
│   └── Program.cs              # App bootstrap, CORS, role seeding
├── OutdoorsShop.Core/          # Domain layer: entities, interfaces, DTOs, enums
│   ├── Entities/               # Product, Customer, SalesOrder, etc.
│   ├── Interfaces/             # Repository & service contracts
│   └── DTOs/                  # Request/response shapes
├── OutdoorsShop.Infrastructure/  # Data access layer
│   ├── Data/                   # AppDbContext (IdentityDbContext), migrations
│   ├── Identity/               # ApplicationUser (extends IdentityUser)
│   ├── Repositories/           # EF Core repository implementations
│   └── Services/               # BlobStorageService, etc.
└── OutdoorsShop.Functions/     # Azure Functions (see §5)
```

### Controllers

| Controller | Base Route | Key Operations |
|---|---|---|
| `AuthController` | `/api/v1/auth` | `POST /register`, `POST /login`, `POST /logout`, `POST /refresh`, `GET /me` |
| `ProductsController` | `/api/v1/products` | CRUD, filter by category, search |
| `CategoriesController` | `/api/v1/categories` | GET all, GET by ID |
| `CustomersController` | `/api/v1/customers` | GET profile, PUT update (Customer role) |
| `OrdersController` | `/api/v1/orders` | POST create, GET list, GET by ID |
| `InventoryController` | `/api/v1/inventory` | GET levels, PUT adjust (Admin) |
| `ReportsController` | `/api/v1/reports` | CSV/Excel exports (Admin) |

### Authentication & Database Details

- **Identity:** `ApplicationUser` extends `IdentityUser` via `IdentityDbContext<ApplicationUser>`
- **Roles seeded at startup:** `Administrator`, `Customer` (via `RoleManager`)
- **JWT tokens:** 15-minute access token; 7-day refresh token in HttpOnly cookie
- **CORS:** `ReactDevPolicy` — origins from `AllowedOrigins__*` app settings. Platform CORS intentionally disabled (ADR — see §11).

### Deployment

- **Host:** Azure App Service (`app-outdoors-api-dev`, westus3, `rg-outdoors-dev`)
- **URL:** `https://app-outdoors-api-dev.azurewebsites.net`
- **Deploy method:** `WEBSITE_RUN_FROM_PACKAGE` pointing to `stoutdoorsdev/webapp-releases/api-dev.zip`
- **Target runtime:** Linux x64, self-contained=false

---

## 5. Azure Functions Architecture

### Technology Stack

| Concern | Detail |
|---|---|
| Runtime | .NET 8 isolated worker |
| Host SDK | `Microsoft.Azure.Functions.Worker` |
| Observability | OpenTelemetry → Azure Monitor exporter |
| Data access | EF Core → Azure SQL (shared `AppDbContext`) |
| Plan | Flex Consumption (always-ready instances) |

### Functions

| Function | Trigger | Queue / Schedule | Purpose |
|---|---|---|---|
| `HealthFunction` | HTTP GET | `/api/health` | Liveness probe |
| `SeasonalDiscountFunction` | Timer | `0 0 2 * * *` (02:00 UTC daily) | Applies seasonal pricing: Winter→Camping/Trekking 15% off; Summer→Cycling/Climbing 10% off; Spring/Autumn→reset |
| `PaymentConfirmationFunction` | Queue | `payment-confirmations` | Marks order as `Processing`/`Cancelled`, restores inventory on failure |
| `StockUpdateFunction` | Queue | `stock-updates` | Adjusts inventory levels, triggers reorder alerts |

Queue connection string comes from `AzureWebJobsStorage` (bound to `stoutdoorsdev` storage account).

### Deployment

- **Host:** Azure Functions App (`func-outdoors-dev`, westus3, `rg-outdoors-dev`)
- **URL:** `https://func-outdoors-dev.azurewebsites.net`
- **Pipeline:** `functions.yml` — build → test → `dotnet publish` → zip deploy via `az functionapp deployment source config-zip`

---

## 6. Data Architecture

### Azure SQL

| Property | Value |
|---|---|
| Server | `azure-sql-pampa.database.windows.net` |
| Database | `OutdoorsShopDB` |
| Resource Group | `AzureSqlRg` |
| Access | EF Core 10 via connection string in Key Vault / App Settings |

### Key Tables

| Table | Entity | Notes |
|---|---|---|
| `Products` | `Product` | `Price decimal(18,2)`, `DiscountMultiplier decimal(5,4)`, `ImageUrl` (Unsplash CDN) |
| `Categories` | `ProductCategory` | Seeded: Camping, Trekking, Cycling, Climbing |
| `Orders` | `SalesOrder` | Status, PaymentStatus, PaymentReference, TotalAmount |
| `OrderItems` | `SalesOrderDetail` | FK to Orders + Products, `UnitPrice decimal(18,2)` |
| `Customers` | `Customer` | Linked to `AspNetUsers` |
| `Inventory` | `ProductInventory` | `QuantityAvailable`, `LastUpdated` |
| `StockUpdateLogs` | `StockUpdateLog` | Audit trail for queue-triggered updates |
| `AspNetUsers` | `ApplicationUser` | ASP.NET Identity table |
| `AspNetRoles` | `IdentityRole` | `Administrator`, `Customer` |

### Migrations

EF Core migrations are in `src/OutdoorsShop.Infrastructure/Data/Migrations/`. Applied against `azure-sql-pampa/OutdoorsShopDB` during initial deployment.

### Seed Data

- `scripts/seed-products.sql` — inserts product catalog rows
- `scripts/update-image-urls.sql` — backfills `ImageUrl` columns with Unsplash CDN URLs
- Product images are stored as Unsplash CDN URLs in the `ImageUrl` column (not uploaded to Blob Storage)

---

## 7. Storage Architecture

### Storage Account: `stoutdoorsdev` ⚠️ DO NOT DELETE

| Container / Queue | Purpose |
|---|---|
| `product-images` | Product image blobs (referenced by `ImageUrl` column) |
| `webapp-releases` | API deployment zip (`api-dev.zip`) for run-from-package |
| Queue: `payment-confirmations` | Messages consumed by `PaymentConfirmationFunction` |
| Queue: `stock-updates` | Messages consumed by `StockUpdateFunction` |

### Storage Account: `stoutdoorswebdev` — Safe to delete

This was the original Blob static website host for the SPA before the SWA migration. The frontend now lives on Azure SWA. This account can be decommissioned once the SWA-based deployment is confirmed stable.

### Planned (Not Implemented)

- Order receipts stored as PDFs in Blob Storage
- CSV/Excel report exports written to Blob Storage by the Reports API

---

## 8. Authentication & Authorization

### Stack

| Layer | Technology |
|---|---|
| Identity provider | ASP.NET Core Identity (`ApplicationUser`) |
| Token format | JWT Bearer (access) + HttpOnly cookie (refresh) |
| Token lifetimes | Access: 15 min · Refresh: 7 days |
| Frontend storage | Access token: Zustand in-memory · Refresh token: HttpOnly cookie |

### Roles

| Role | Permissions |
|---|---|
| `Administrator` | Full CRUD on products/categories/inventory; order management; reports |
| `Customer` | Browse products, manage own cart/orders/profile |

Roles are seeded at API startup in `Program.cs` using `RoleManager<IdentityRole>`.

### Registration

`RegisterDto` fields: `name`, `email`, `password`, `confirmPassword`. The `name` field maps to a single display name (not split into firstName/lastName — see §12 Known Gaps).

### Token Flow

```
POST /api/v1/auth/login
  → 200: { accessToken } + Set-Cookie: refreshToken (HttpOnly)

POST /api/v1/auth/refresh  (cookie auto-sent)
  → 200: { accessToken }  (new access token)

POST /api/v1/auth/logout
  → 200: clears refresh token cookie
```

---

## 9. CI/CD Pipeline

### Workflows

| File | Trigger | What it does |
|---|---|---|
| `frontend.yml` | push/PR to `main`/`dev` on `frontend/**` | `npm ci` → `npm run build` → SWA deploy (`Azure/static-web-apps-deploy@v1`) |
| `backend.yml` | push/PR to `main`/`dev` on `src/**` | `dotnet restore` → `dotnet build` → `dotnet test` → upload TRX results |
| `functions.yml` | push/PR to `main`/`dev` on `src/OutdoorsShop.Functions/**` | build → test → `dotnet publish` (linux-x64) → zip → `az functionapp deployment source config-zip` |

> **Note:** `backend.yml` runs CI (build + test) but does not automatically deploy the API. Deployment uses the run-from-package blob strategy (see §4).

### Authentication to Azure

All workflows use OIDC federated credentials (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id` secrets). No service principal secrets are stored in the repository.

### Branch Strategy

```
feature/* ──► dev (integration, status checks only)
                │
                └──► main (production, PR + approval + status checks required)
```

A `main` branch worktree checkout is maintained at `.copilot-main/` for multi-branch operations.

---

## 10. Azure Resources

| Resource Name | Type | Region | Resource Group | Notes |
|---|---|---|---|---|
| `app-outdoorsweb-swa` | Azure Static Web App | westus3 | rg-outdoors-dev | Frontend host; URL: `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net` |
| `app-outdoors-api-dev` | App Service (Linux) | westus3 | rg-outdoors-dev | .NET 10 Web API |
| `func-outdoors-dev` | Azure Functions App | westus3 | rg-outdoors-dev | .NET 8 isolated; Flex Consumption plan |
| `azure-sql-pampa` | Azure SQL Server | (existing) | AzureSqlRg | Hosts `OutdoorsShopDB` |
| `OutdoorsShopDB` | Azure SQL Database | — | AzureSqlRg | EF Core target |
| `stoutdoorsdev` | Storage Account | westus3 | rg-outdoors-dev | ⚠️ DO NOT DELETE — images, queues, releases |
| `stoutdoorswebdev` | Storage Account | westus3 | rg-outdoors-dev | Old SPA host — safe to delete |
| Key Vault | Azure Key Vault | westus3 | rg-outdoors-dev | Secrets for API + Functions |
| App Insights / Monitor | Azure Monitor | westus3 | rg-outdoors-dev | Observability via OpenTelemetry |

---

## 11. Architecture Decision Records (ADRs)

| ADR | Title | Decision | Status |
|---|---|---|---|
| ADR-001 | Monorepo structure | Single repo with `src/`, `frontend/`, `infra/` directories | Accepted |
| ADR-002 | .NET Clean Architecture | Core → Infrastructure → API layering; no circular dependencies | Accepted |
| ADR-003 | JWT + ASP.NET Core Identity | JWT access tokens (15 min) + HttpOnly refresh cookie (7 days); ASP.NET Identity for user/role management | Accepted |
| ADR-004 | Client-side cart | Cart stored in Zustand + localStorage; no `Cart` table in DB; submitted to API only at checkout | Accepted |
| ADR-005 | EF Core 10 + repository pattern + Mapster | Repository pattern abstracts data access; Mapster handles entity↔DTO mapping | Accepted |
| ADR-006 | Key Vault + managed identity | Zero plaintext secrets in code or config; all secrets sourced from Key Vault via managed identity | Accepted |
| — | Azure region: westus3 | All new Azure resources deployed in `westus3`; `eastus` avoided due to quota constraints | Accepted |
| — | CORS: app middleware only | CORS enforced exclusively in ASP.NET Core middleware (`ReactDevPolicy`); Azure App Service platform CORS disabled to prevent dual-enforcement conflict | Accepted |
| — | Frontend: SWA over Blob static website | Azure SWA provides correct HTTP 200 on all SPA routes; Blob static website returns HTTP 404 on deep links | Accepted |
| — | Reuse existing SQL server | `azure-sql-pampa/OutdoorsShopDB` reused instead of provisioning a new SQL server; `deploySql=false` in Bicep | Accepted |
| — | Run-from-package deployment | API deployed via `WEBSITE_RUN_FROM_PACKAGE` pointing to `stoutdoorsdev/webapp-releases/api-dev.zip` | Accepted |

---

## 12. Known Gaps & Future Work

| Item | Description |
|---|---|
| `RegisterDto` name field | Current `RegisterDto` uses a single `name` field. Some consumers may expect `firstName`/`lastName`. Standardize before adding profile editing. |
| Cart → Checkout E2E | Full cart-to-confirmation flow requires Playwright end-to-end tests. Currently covered only at unit/integration level. |
| CSV/Excel report exports | `ReportsController` is implemented but Blob Storage write-back for exports is not yet wired up. |
| `stoutdoorswebdev` cleanup | Old Blob static website storage account can be safely deleted after confirming SWA is the stable frontend host. |
| `func-outdoors-dev` health | Functions app returned `503` on initial provisioning. Needs follow-up verification that the Flex Consumption plan is correctly configured. |
| CORS origins update | After SWA migration, `AllowedOrigins__*` on `app-outdoors-api-dev` should be updated to include the SWA hostname and remove the old `stoutdoorswebdev` origin. |
| Backend CI deploy automation | `backend.yml` only runs CI. API deployment is currently a manual blob-upload step. Automating this in the workflow is desirable. |
