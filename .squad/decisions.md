# Squad Decisions

## Active Decisions

---

### 2026-05-23: Project Initialized

**By:** Jorgito (via Squad Coordinator)  
**What:** Outdoors Shop project initialized. Team hired from The Wind-Up Bird Chronicle universe: Toru (Architect), Cinnamon (Backend Developer), Malta (Frontend Developer), Creta (Test Suite), Scribe (Documentation), Ralph (Work Monitor). Squad v0.9.4.  
**Why:** New project kickoff.  

---

### 2026-05-23: Branch Strategy

**By:** Jorgito  
**What:** `dev` is the active development branch. `main` is for production deployment. All squad feature branches follow `squad/{issue-number}-{slug}` convention and target `dev`.  
**Why:** Specified in project brief. Standard feature-branch promotion: feature → dev → main for release.  

---

### 2026-05-23: Tech Stack Confirmed

**By:** Jorgito  
**What:** React + TypeScript (frontend), .NET 10 Web API C# ASP.NET Core (backend), Azure SQL Database with Adventure Works-inspired schema (data), Azure Functions .NET isolated (serverless auxiliary tasks), Azure Blob Storage (assets and reports), JWT role-based auth with roles Administrator and Customer.  
**Why:** Specified in project brief. Baseline for all implementation decisions.  

---

### 2026-05-23: Domain Model Scope

**By:** Jorgito  
**What:** Core entities are Products, Categories (Camping, Trekking, Cycling, Climbing), Customers, Orders, and Inventory. Inspired by Adventure Works simplified schema on Azure SQL Database.  
**Why:** Specified in project brief. Cinnamon owns the data model design under Toru's architecture review.  

---

### 2026-05-23: Azure Function Scope

**By:** Jorgito  
**What:** Azure Functions handle three auxiliary tasks: seasonal discounts (scheduled), payment confirmation (event-driven), stock updates (event-driven).  
**Why:** Specified in project brief. Keeps the Web API lean — background/async work lives in Functions.  

---

### 2026-05-23: Storage Account Scope

**By:** Jorgito  
**What:** Azure Storage Account (Blob) stores product images, order receipts, and exported reports (CSV/Excel). The Web API integrates with the storage account for read/write operations.  
**Why:** Specified in project brief.  

---

### 2026-05-23: Monorepo folder structure adopted
**By:** Toru (Architect)
**What:** Single Git repository with three top-level source areas: `src/` (.NET projects), `frontend/` (React + TypeScript), and `infra/` (Bicep IaC). A shared `OutdoorsShop.sln` ties all .NET projects together. Azure Functions live in `src/OutdoorsShop.Functions/` as an isolated-process project.
**Why:** Keeps the full stack version-aligned in one repo. Simplifies cross-concern refactors and CI/CD. Avoids submodule complexity for a single-team PoC.

---

### 2026-05-23: .NET project layering (Api / Core / Infrastructure / Functions / Tests)
**By:** Toru (Architect)
**What:** Five .NET projects: `OutdoorsShop.Api` (controllers, middleware, startup), `OutdoorsShop.Core` (domain entities, interfaces — no dependencies), `OutdoorsShop.Infrastructure` (EF Core, repositories, storage clients), `OutdoorsShop.Functions` (Azure Functions isolated), `OutdoorsShop.Tests` (xUnit).
**Why:** Classic onion/clean separation. Core has zero external dependencies, making unit testing trivial. Infrastructure is the only layer that touches EF Core and Azure SDKs.

---

### 2026-05-23: React app scaffolded with Vite + TypeScript
**By:** Toru (Architect)
**What:** React frontend lives in `frontend/` and is scaffolded with `npm create vite@latest -- --template react-ts`. Build output goes to `frontend/dist/`.
**Why:** Vite is the current standard React toolchain. Fast HMR for development, clean static output for deployment.

---

### 2026-05-23: Adventure Works-inspired schema — six core tables
**By:** Toru (Architect)
**What:** Schema has six application tables: `Categories`, `Products`, `Customers`, `Orders`, `OrderItems`, `Inventory`. Identity (users/roles) uses ASP.NET Core Identity default tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). `Customers.UserId` is a FK to `AspNetUsers.Id`.
**Why:** Separates authentication identity (ASP.NET Identity) from business domain (Customers). A Customer record is created upon registration and linked to the Identity user. This pattern aligns with Adventure Works's Person/Customer split.

---

### 2026-05-23: EF Core with repository pattern via OutdoorsShop.Infrastructure
**By:** Toru (Architect)
**What:** EF Core 10 is the ORM. DbContext lives in `OutdoorsShop.Infrastructure`. Repositories implement interfaces defined in `OutdoorsShop.Core`. No raw ADO.NET in application code. Migrations managed via EF Core CLI (`dotnet ef migrations`).
**Why:** Repository pattern decouples controllers from the ORM, making unit testing with in-memory providers or mocks straightforward. Cinnamon owns implementation; interfaces in Core enforce the contract.

---

### 2026-05-23: Soft deletes via IsActive flag on Products and Categories
**By:** Toru (Architect)
**What:** `Products.IsActive` and `Categories.IsActive` columns implement logical delete. Physical DELETE is not used for these tables. All queries filter `WHERE IsActive = 1` by default via EF Core global query filters.
**Why:** Product removal from catalog must not break historical order records that reference the product. Soft delete preserves referential integrity.

---

### 2026-05-23: Inventory table ReorderThreshold for stock alerts
**By:** Toru (Architect)
**What:** `Inventory.ReorderThreshold` is an INT column. When the `StockUpdateQueue` function reduces quantity, it checks if `Quantity <= ReorderThreshold` and can raise an alert (logged to App Insights for PoC; extensible to email later).
**Why:** Business requirement: inventory tracking. Threshold makes the stock-update function actionable without hard-coded logic.

---

### 2026-05-23: REST API base path /api/v1 with versioning from day one
**By:** Toru (Architect)
**What:** All Web API routes are prefixed `/api/v1/`. ASP.NET Core API versioning middleware (`Asp.Versioning`) is installed from the start. Current version is v1. Future breaking changes go to v2 without disrupting existing clients.
**Why:** Adding versioning retroactively is painful. Zero cost to add it now; v1 prefix becomes the permanent base.

---

### 2026-05-23: Six resource groups — Products, Categories, Customers, Orders, Inventory, Auth
**By:** Toru (Architect)
**What:** Controllers: `ProductsController`, `CategoriesController`, `CustomersController`, `OrdersController`, `InventoryController`, `AuthController`. A `ReportsController` is added for CSV/Excel export endpoints. Each controller maps exactly to one infrastructure concern.
**Why:** Matches the domain model 1:1. No "god controller." Easy for Cinnamon to implement one controller per sprint.

---

### 2026-05-23: OpenAPI/Swagger via Swashbuckle with XML doc comments
**By:** Toru (Architect)
**What:** Swashbuckle.AspNetCore added to `OutdoorsShop.Api`. XML documentation generated (`GenerateDocumentationFile = true` in .csproj). Swagger UI available at `/swagger` in dev only (disabled in prod). All endpoints annotated with `[ProducesResponseType]`.
**Why:** Swagger UI is the integration reference for Malta (Frontend). Disabling in prod avoids exposing the API surface to the public.

---

### 2026-05-23: Cart is client-side state, not server-side
**By:** Toru (Architect)
**What:** No `Cart` or `CartItems` table in the database. The shopping cart lives entirely in the React frontend (localStorage + Zustand store). On checkout, the frontend sends a `POST /api/v1/orders` with the full order payload.
**Why:** Simplifies the backend significantly for a PoC. No session management or abandoned-cart cleanup needed. Cart state survives page reload via localStorage persistence.

---

### 2026-05-23: ASP.NET Core Identity + JWT bearer tokens
**By:** Toru (Architect)
**What:** Authentication uses ASP.NET Core Identity for user/password/role management backed by Azure SQL. JWT bearer tokens are issued by the API's `AuthController` using `System.IdentityModel.Tokens.Jwt`. No third-party identity provider (no Entra ID, no Auth0) for PoC.
**Why:** Self-contained auth keeps the PoC infrastructure minimal. ASP.NET Core Identity is the standard .NET 10 approach. JWT is stateless and naturally fits React SPA + API architecture.

---

### 2026-05-23: JWT access token 15 min, refresh token 7 days stored in HttpOnly cookie
**By:** Toru (Architect)
**What:** Access tokens expire in 15 minutes. Refresh tokens expire in 7 days and are issued as `HttpOnly`, `Secure`, `SameSite=Strict` cookies. `POST /api/v1/auth/refresh` accepts the cookie and issues a new access token. Refresh tokens are stored hashed in `AspNetUserTokens`.
**Why:** Short-lived access tokens limit exposure window. HttpOnly cookie for refresh prevents XSS token theft. Storing refresh token hash in Identity's UserTokens table enables server-side revocation.

---

### 2026-05-23: Two roles — Administrator and Customer
**By:** Toru (Architect)
**What:** Role `Administrator`: full access to all endpoints including `GET/POST/PUT/DELETE` on products, categories, inventory, all orders, and reports. Role `Customer`: read products/categories, manage own orders (`GET/POST` on own orders only), read own customer profile (`GET/PUT`). No anonymous access beyond product/category browsing.
**Why:** Matches the project brief exactly. RBAC enforced at the controller level via `[Authorize(Roles = "Administrator")]` and `[Authorize]` attributes. Fine-grained resource ownership (customers seeing only their orders) enforced in service layer by comparing `CustomerId` from JWT `sub` claim.

---

### 2026-05-23: JWT claims structure
**By:** Toru (Architect)
**What:** JWT payload includes: `sub` (UserId GUID), `email`, `role` (Administrator | Customer), `given_name`, `family_name`, `customer_id` (CustomerId INT — added as custom claim for Customers), `jti` (unique token ID), `iss` (issuer), `aud` (audience), `exp`, `iat`.
**Why:** `customer_id` custom claim avoids a DB round-trip to resolve UserId → CustomerId on every request. `jti` enables token blacklisting if needed. Standard claims (`iss`, `aud`) required for proper JWT validation.

---

### 2026-05-23: Frontend stores access token in memory (not localStorage)
**By:** Toru (Architect)
**What:** The React app stores the access token in a Zustand auth store (in-memory, not persisted to localStorage or sessionStorage). On page refresh, the app calls `POST /api/v1/auth/refresh` using the HttpOnly cookie to silently re-issue the token.
**Why:** Storing JWTs in localStorage exposes them to XSS. In-memory + HttpOnly cookie refresh is the current best practice for SPA authentication security.

---

### 2026-05-23: Azure resource naming convention
**By:** Toru (Architect)
**What:** Pattern `{abbreviation}-outdoors-{environment}` (e.g., `app-outdoors-api-dev`, `kv-outdoors-prod`). Storage accounts use no hyphens due to Azure limits: `stoutdoorsdev`, `stoutdoorsprod`. Two resource groups: `rg-outdoors-dev` and `rg-outdoors-prod`.
**Why:** Predictable names reduce lookup friction. Abbreviations follow Microsoft CAF (Cloud Adoption Framework) conventions: `app`, `asp`, `sql`, `sqldb`, `st`, `func`, `kv`, `appi`, `law`.

---

### 2026-05-23: App Service B1 for dev, P2v3 for prod
**By:** Toru (Architect)
**What:** Web API hosted on Azure App Service. Dev uses B1 (Basic) plan. Production uses P2v3 (Premium v3) for auto-scale readiness. No VNet or private endpoints for PoC.
**Why:** App Service over Container Apps for simplicity — no container registry or orchestration overhead for a PoC. P2v3 chosen over P1v3 for baseline production memory headroom.

---

### 2026-05-23: Azure SQL Basic/S0 for dev, S2 for prod
**By:** Toru (Architect)
**What:** Azure SQL Database on the DTU model. Dev: Basic (5 DTU, sufficient for seeding and testing). Prod: S2 (50 DTU) for concurrent user load. Both use geo-redundant backup.
**Why:** DTU model is simpler to reason about for a PoC. S2 prod gives 50 DTU and 250 GB — enough for the domain scope.

---

### 2026-05-23: Azure Functions on Consumption plan
**By:** Toru (Architect)
**What:** Functions App uses the Consumption (Y1) hosting plan for both dev and prod.
**Why:** The three functions (discount timer, payment queue, stock queue) are infrequent. Consumption cost is near-zero for PoC traffic. Scale-to-zero is acceptable for auxiliary background tasks.

---

### 2026-05-23: Key Vault with managed identity access
**By:** Toru (Architect)
**What:** All secrets (connection strings, JWT signing key, Storage SAS token) stored in Azure Key Vault. App Service and Functions App access Key Vault via system-assigned managed identity with `Key Vault Secrets User` role. No connection strings in app settings or code.
**Why:** Eliminates secret rotation risk and credential exposure in CI/CD logs. Managed identity removes the need to manage service principal credentials.

---

### 2026-05-23: Storage Account containers and access levels
**By:** Toru (Architect)
**What:** Three Blob containers: `product-images` (Blob-level public read — product images are public), `order-receipts` (private — SAS URL generated by API on request), `exports` (private — SAS URL generated by API on download).
**Why:** Product images need public CDN-friendly URLs. Receipts and exports contain PII and must not be publicly listable.

---

### 2026-05-23: Three GitHub Actions workflows — backend, frontend, functions
**By:** Toru (Architect)
**What:** Three workflow files: `.github/workflows/backend.yml` (triggers on `src/OutdoorsShop.Api/**` and `src/OutdoorsShop.Core/**` and `src/OutdoorsShop.Infrastructure/**` changes), `.github/workflows/frontend.yml` (triggers on `frontend/**`), `.github/workflows/functions.yml` (triggers on `src/OutdoorsShop.Functions/**`). Each workflow runs on push to `dev` (deploy to dev environment) and push to `main` (deploy to prod environment).
**Why:** Scoped path filters prevent a frontend change from triggering a backend deployment. Independent pipelines allow the teams to move at different speeds.

---

### 2026-05-23: Secrets in GitHub Environments (dev / prod) using OIDC federated credentials
**By:** Toru (Architect)
**What:** GitHub Environments `dev` and `prod` are configured. Azure authentication uses OIDC (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id`) — no service principal secret stored. Application secrets (connection strings, JWT key) are stored in GitHub Environment secrets and injected as Azure App Service application settings at deploy time (not checked into code).
**Why:** OIDC eliminates the need to rotate service principal secrets. GitHub Environments provide environment-scoped secret isolation (prod secrets are not accessible from dev pipeline runs).

---

### 2026-05-23: Branch protection rules
**By:** Toru (Architect)
**What:** `main` branch: required status checks (backend build+test, frontend build+test), required PR with 1 approval, no direct push. `dev` branch: required status checks (build+test pass), direct push allowed for squad agents (to unblock flow during PoC). Squad feature branches follow `squad/{slug}` → PR → `dev`.
**Why:** `main` is protected to prevent broken production deployments. `dev` protection ensures tests pass before integration but allows direct push for squad agility during PoC phase.

---

### 2026-05-23: CI pipeline steps — build, test, migrate, deploy
**By:** Toru (Architect)
**What:** Backend pipeline steps: (1) `dotnet restore`, (2) `dotnet build --no-restore`, (3) `dotnet test --no-build --collect:"XPlat Code Coverage"`, (4) `dotnet publish -c Release -o ./publish`, (5) `dotnet ef database update` (dev only — prod migrations are script-based review), (6) `az webapp deploy`. Functions pipeline mirrors steps 1–4 then `az functionapp deployment source config-zip`.
**Why:** EF migrations run automatically on dev for speed. On prod, a reviewed SQL script is applied manually (or via deployment slot swap) to prevent accidental schema destruction. Code coverage collected for visibility without blocking the pipeline.

---

# ADR-001: Monorepo Folder Structure

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Adopt a single Git repository with three top-level source areas:

```
/src/           .NET projects (Api, Core, Infrastructure, Functions)
/frontend/      React + TypeScript (Vite)
/infra/         Bicep infrastructure-as-code
/tests/         .NET test projects (Api.Tests, Functions.Tests)
/frontend/tests Vitest + React Testing Library
/.github/       GitHub Actions workflows
```

A single `OutdoorsShop.sln` at repo root ties all .NET projects together.

## Rationale

A monorepo keeps the entire stack version-aligned in one repository. Cross-concern refactors (e.g., renaming a DTO that affects both the API and frontend) are atomic. It simplifies CI/CD configuration — all pipelines live in one `.github/workflows/` directory with path-based filters. Avoids submodule complexity for a single-team PoC.

## Consequences

- All developers clone one repository.
- CI workflows must use `paths:` filters to avoid rebuilding unaffected layers on every push.
- Frontend and backend can be deployed independently despite living in the same repo.

---

# ADR-002: .NET Clean Architecture Layering

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Organise the .NET solution into four projects following clean/onion architecture:

| Project | Layer | Dependencies |
|---|---|---|
| `OutdoorsShop.Core` | Domain | None |
| `OutdoorsShop.Infrastructure` | Data/Services | Core |
| `OutdoorsShop.Api` | Presentation | Core, Infrastructure |
| `OutdoorsShop.Functions` | Background | Core, Infrastructure |

`OutdoorsShop.Core` contains: domain entities, repository interfaces, service interfaces, DTOs, enums, and domain exceptions. It references no NuGet packages with external runtime dependencies.

`OutdoorsShop.Infrastructure` contains: EF Core `DbContext`, repository implementations, Azure Storage clients, email stub clients.

`OutdoorsShop.Api` contains: controllers, middleware, program startup, request/response models, validators (FluentValidation).

`OutdoorsShop.Functions` is an isolated-process Azure Functions project. It references Core and Infrastructure but is deployed independently.

## Rationale

Classic onion separation. Core having zero external dependencies makes domain logic trivially unit-testable with no mocking of infrastructure. Infrastructure is the only layer that touches EF Core and Azure SDKs, so switching persistence or cloud provider is isolated to one project.

## Consequences

- Circular dependency between Core and Infrastructure is forbidden by compiler (Core cannot reference Infrastructure).
- Dependency injection wires Infrastructure implementations to Core interfaces at startup in `OutdoorsShop.Api`.
- `OutdoorsShop.Functions` re-uses the same DI wiring pattern via `HostBuilder`.

---

# ADR-003: JWT Bearer Authentication with ASP.NET Core Identity

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Use **ASP.NET Core Identity** for user/role management combined with **JWT bearer tokens** for stateless API authentication.

**Token strategy:**
- Access token: RS256 signed, 15-minute expiry, stored in JavaScript memory (not localStorage).
- Refresh token: opaque random value, 7-day expiry, stored in an `HttpOnly; Secure; SameSite=Strict` cookie. Hashed SHA-256 before storage in `AspNetUserTokens`.

**Roles:** Two roles — `Administrator` and `Customer`.

**Custom JWT claims:**
- `customer_id` (GUID): maps the Identity user to the business Customer record. Avoids a DB round-trip on every authenticated request.
- `email`, `role` (standard claims).

**Refresh flow:** `POST /api/v1/auth/refresh` reads the HttpOnly cookie, validates the hashed token from `AspNetUserTokens`, issues a new access token and rotates the refresh token.

## Rationale

ASP.NET Core Identity provides battle-tested user/role management, password hashing, and account lockout out of the box. JWT bearer allows stateless horizontal scaling of the API without sticky sessions. Storing the access token in memory and the refresh token in an HttpOnly cookie is the recommended browser security pattern — it eliminates XSS access to long-lived tokens.

## Consequences

- The React frontend must implement a silent token refresh mechanism (intercepting 401 responses to call `/auth/refresh` before retrying).
- `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles` tables are created by Identity migrations.
- JWT signing key must be stored in Azure Key Vault, never in `appsettings.json`.

---

# ADR-004: Client-Side Cart with No Database Persistence

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

The shopping cart is managed entirely on the client side using **Zustand** state management with **localStorage** persistence. There is no `Cart` or `CartItem` table in the database.

Cart state is only persisted to the backend when the customer places an order — at that point the cart contents become `OrderItems` records in the database.

The `/api/v1/cart` endpoint group defined in the API contract is a thin pass-through for **order placement only**: `POST /api/v1/cart/checkout` converts the client-submitted cart into an Order.

## Rationale

A server-side cart adds significant database complexity (cart expiry, anonymous cart merging on login, orphaned records cleanup) for a PoC that does not require it. Client-side cart via Zustand + localStorage satisfies the business requirement of persisting the cart between browser sessions without backend infrastructure. This is consistent with how many small-to-medium e-commerce frontends operate.

## Consequences

- Cart state is lost if the customer switches browsers or devices — acceptable for PoC scope.
- `POST /api/v1/cart/checkout` accepts the full cart payload and creates an Order transactionally.
- A `Cart` table can be added in a future iteration without changing the existing Order API.
- Malta (Frontend) owns cart state management in `frontend/src/store/cartStore.ts`.

---

# ADR-005: EF Core 10 with Repository Pattern

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Use **EF Core 10** as the ORM with a **repository pattern** enforced via interfaces.

- Repository interfaces are defined in `OutdoorsShop.Core/Interfaces/` (e.g., `IProductRepository`, `IOrderRepository`).
- Concrete implementations live in `OutdoorsShop.Infrastructure/Repositories/`.
- `OutdoorsShopDbContext` lives in `OutdoorsShop.Infrastructure/Data/`.
- Migrations are managed via EF Core CLI (`dotnet ef migrations add`). Migration files are committed to source control.
- No raw ADO.NET in application code. Stored procedures are not used in this PoC.

**Global Query Filters applied:**
- `Products`: `WHERE IsActive = 1`
- `Categories`: `WHERE IsActive = 1`

**Mapping:** Manual mapping or **Mapster** for entity → DTO projection. AutoMapper is explicitly excluded due to runtime magic complexity.

## Rationale

EF Core provides type-safe LINQ queries, migrations, and change tracking without raw SQL. Repository interfaces in Core decouple the application from the ORM, making unit testing straightforward with `InMemoryDatabase` or mock repositories. Mapster was chosen over AutoMapper because it is faster at runtime, has a simpler configuration model, and its compile-time mapping generation eliminates the AutoMapper "no mapping found" class of runtime errors.

## Consequences

- Cinnamon (Backend) owns DbContext configuration and migration authoring.
- Tests use `UseInMemoryDatabase` provider for repository unit tests.
- Adding a new entity requires: entity in Core, interface in Core, migration in Infrastructure, registration in DI.

---

# ADR-006: Azure Key Vault with Managed Identity for Secrets

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

All application secrets (database connection strings, JWT signing key, Storage Account keys) are stored in **Azure Key Vault**. The App Service and Functions App access Key Vault using **system-assigned managed identities** — no service principal credentials or connection strings appear in `appsettings.json` or application settings.

**Key Vault naming:** `kv-outdoors-{env}` (e.g., `kv-outdoors-dev`).

**Secret names (convention):** `{resource}--{property}` using double-dash to match ASP.NET Core configuration hierarchy (e.g., `ConnectionStrings--DefaultConnection`, `Jwt--SigningKey`).

**Local development:** Developers use `dotnet user-secrets` and Azure CLI authentication (`az login`). The `DefaultAzureCredential` chain handles both local and production scenarios transparently.

## Rationale

Secrets in source control or app settings environment variables are a primary attack surface (OWASP A02: Cryptographic Failures / Secrets Exposure). Managed identity eliminates long-lived credentials entirely — there is nothing to rotate, leak, or store. This is the Azure-recommended pattern for App Service and Functions workloads. `DefaultAzureCredential` allows the same code to work locally (via Azure CLI token) and in production (via managed identity) without any conditional logic.

## Consequences

- The App Service and Functions App managed identities must be granted `Key Vault Secrets User` role on the Key Vault (RBAC, not vault access policies — access policies are legacy).
- Key Vault is provisioned in `infra/modules/keyvault.bicep` before the App Service is deployed.
- Rotation of the JWT signing key triggers a brief window where existing access tokens are invalid — acceptable for PoC; document as known limitation.
- Local developers need `az login` and Key Vault `Secrets User` assignment on their Azure user identity.

---

### 2026-05-23: REST API base path /api/v1 with versioning from day one
**By:** Toru (Architect)
**What:** All Web API routes are prefixed `/api/v1/`. ASP.NET Core API versioning middleware (`Asp.Versioning`) is installed from the start. Current version is v1. Future breaking changes go to v2 without disrupting existing clients.
**Why:** Adding versioning retroactively is painful. Zero cost to add it now; v1 prefix becomes the permanent base.

### 2026-05-23: Six resource groups — Products, Categories, Customers, Orders, Inventory, Auth
**By:** Toru (Architect)
**What:** Controllers: `ProductsController`, `CategoriesController`, `CustomersController`, `OrdersController`, `InventoryController`, `AuthController`. A `ReportsController` is added for CSV/Excel export endpoints. Each controller maps exactly to one infrastructure concern.
**Why:** Matches the domain model 1:1. No "god controller." Easy for Cinnamon to implement one controller per sprint.

### 2026-05-23: OpenAPI/Swagger via Swashbuckle with XML doc comments
**By:** Toru (Architect)
**What:** Swashbuckle.AspNetCore added to `OutdoorsShop.Api`. XML documentation generated (`GenerateDocumentationFile = true` in .csproj). Swagger UI available at `/swagger` in dev only (disabled in prod). All endpoints annotated with `[ProducesResponseType]`.
**Why:** Swagger UI is the integration reference for Malta (Frontend). Disabling in prod avoids exposing the API surface to the public.

### 2026-05-23: Cart is client-side state, not server-side
**By:** Toru (Architect)
**What:** No `Cart` or `CartItems` table in the database. The shopping cart lives entirely in the React frontend (localStorage + Zustand store). On checkout, the frontend sends a `POST /api/v1/orders` with the full order payload.
**Why:** Simplifies the backend significantly for a PoC. No session management or abandoned-cart cleanup needed. Cart state survives page reload via localStorage persistence.

---

### 2026-05-23: ASP.NET Core Identity + JWT bearer tokens
**By:** Toru (Architect)
**What:** Authentication uses ASP.NET Core Identity for user/password/role management backed by Azure SQL. JWT bearer tokens are issued by the API's `AuthController` using `System.IdentityModel.Tokens.Jwt`. No third-party identity provider (no Entra ID, no Auth0) for PoC.
**Why:** Self-contained auth keeps the PoC infrastructure minimal. ASP.NET Core Identity is the standard .NET 10 approach. JWT is stateless and naturally fits React SPA + API architecture.

### 2026-05-23: JWT access token 15 min, refresh token 7 days stored in HttpOnly cookie
**By:** Toru (Architect)
**What:** Access tokens expire in 15 minutes. Refresh tokens expire in 7 days and are issued as `HttpOnly`, `Secure`, `SameSite=Strict` cookies. `POST /api/v1/auth/refresh` accepts the cookie and issues a new access token. Refresh tokens are stored hashed in `AspNetUserTokens`.
**Why:** Short-lived access tokens limit exposure window. HttpOnly cookie for refresh prevents XSS token theft. Storing refresh token hash in Identity's UserTokens table enables server-side revocation.

### 2026-05-23: Two roles — Administrator and Customer
**By:** Toru (Architect)
**What:** Role `Administrator`: full access to all endpoints including `GET/POST/PUT/DELETE` on products, categories, inventory, all orders, and reports. Role `Customer`: read products/categories, manage own orders (`GET/POST` on own orders only), read own customer profile (`GET/PUT`). No anonymous access beyond product/category browsing.
**Why:** Matches the project brief exactly. RBAC enforced at the controller level via `[Authorize(Roles = "Administrator")]` and `[Authorize]` attributes. Fine-grained resource ownership (customers seeing only their orders) enforced in service layer by comparing `CustomerId` from JWT `sub` claim.

### 2026-05-23: JWT claims structure
**By:** Toru (Architect)
**What:** JWT payload includes: `sub` (UserId GUID), `email`, `role` (Administrator | Customer), `given_name`, `family_name`, `customer_id` (CustomerId INT — added as custom claim for Customers), `jti` (unique token ID), `iss` (issuer), `aud` (audience), `exp`, `iat`.
**Why:** `customer_id` custom claim avoids a DB round-trip to resolve UserId → CustomerId on every request. `jti` enables token blacklisting if needed. Standard claims (`iss`, `aud`) required for proper JWT validation.

### 2026-05-23: Frontend stores access token in memory (not localStorage)
**By:** Toru (Architect)
**What:** The React app stores the access token in a Zustand auth store (in-memory, not persisted to localStorage or sessionStorage). On page refresh, the app calls `POST /api/v1/auth/refresh` using the HttpOnly cookie to silently re-issue the token.
**Why:** Storing JWTs in localStorage exposes them to XSS. In-memory + HttpOnly cookie refresh is the current best practice for SPA authentication security.

---

### 2026-05-23: Azure resource naming convention
**By:** Toru (Architect)
**What:** Pattern `{abbreviation}-outdoors-{environment}` (e.g., `app-outdoors-api-dev`, `kv-outdoors-prod`). Storage accounts use no hyphens due to Azure limits: `stoutdoorsdev`, `stoutdoorsprod`. Two resource groups: `rg-outdoors-dev` and `rg-outdoors-prod`.
**Why:** Predictable names reduce lookup friction. Abbreviations follow Microsoft CAF (Cloud Adoption Framework) conventions: `app`, `asp`, `sql`, `sqldb`, `st`, `func`, `kv`, `appi`, `law`.

### 2026-05-23: App Service B1 for dev, P2v3 for prod
**By:** Toru (Architect)
**What:** Web API hosted on Azure App Service. Dev uses B1 (Basic) plan. Production uses P2v3 (Premium v3) for auto-scale readiness. No VNet or private endpoints for PoC.
**Why:** App Service over Container Apps for simplicity — no container registry or orchestration overhead for a PoC. P2v3 chosen over P1v3 for baseline production memory headroom.

### 2026-05-23: Azure SQL Basic/S0 for dev, S2 for prod
**By:** Toru (Architect)
**What:** Azure SQL Database on the DTU model. Dev: Basic (5 DTU, sufficient for seeding and testing). Prod: S2 (50 DTU) for concurrent user load. Both use geo-redundant backup.
**Why:** DTU model is simpler to reason about for a PoC. S2 prod gives 50 DTU and 250 GB — enough for the domain scope.

### 2026-05-23: Azure Functions on Consumption plan
**By:** Toru (Architect)
**What:** Functions App uses the Consumption (Y1) hosting plan for both dev and prod.
**Why:** The three functions (discount timer, payment queue, stock queue) are infrequent. Consumption cost is near-zero for PoC traffic. Scale-to-zero is acceptable for auxiliary background tasks.

### 2026-05-23: Key Vault with managed identity access
**By:** Toru (Architect)
**What:** All secrets (connection strings, JWT signing key, Storage SAS token) stored in Azure Key Vault. App Service and Functions App access Key Vault via system-assigned managed identity with `Key Vault Secrets User` role. No connection strings in app settings or code.
**Why:** Eliminates secret rotation risk and credential exposure in CI/CD logs. Managed identity removes the need to manage service principal credentials.

### 2026-05-23: Storage Account containers and access levels
**By:** Toru (Architect)
**What:** Three Blob containers: `product-images` (Blob-level public read — product images are public), `order-receipts` (private — SAS URL generated by API on request), `exports` (private — SAS URL generated by API on download).
**Why:** Product images need public CDN-friendly URLs. Receipts and exports contain PII and must not be publicly listable.

---

### 2026-05-23: Three GitHub Actions workflows — backend, frontend, functions
**By:** Toru (Architect)
**What:** Three workflow files: `.github/workflows/backend.yml` (triggers on `src/OutdoorsShop.Api/**` and `src/OutdoorsShop.Core/**` and `src/OutdoorsShop.Infrastructure/**` changes), `.github/workflows/frontend.yml` (triggers on `frontend/**`), `.github/workflows/functions.yml` (triggers on `src/OutdoorsShop.Functions/**`). Each workflow runs on push to `dev` (deploy to dev environment) and push to `main` (deploy to prod environment).
**Why:** Scoped path filters prevent a frontend change from triggering a backend deployment. Independent pipelines allow the teams to move at different speeds.

### 2026-05-23: Secrets in GitHub Environments (dev / prod) using OIDC federated credentials
**By:** Toru (Architect)
**What:** GitHub Environments `dev` and `prod` are configured. Azure authentication uses OIDC (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id`) — no service principal secret stored. Application secrets (connection strings, JWT key) are stored in GitHub Environment secrets and injected as Azure App Service application settings at deploy time (not checked into code).
**Why:** OIDC eliminates the need to rotate service principal secrets. GitHub Environments provide environment-scoped secret isolation (prod secrets are not accessible from dev pipeline runs).

### 2026-05-23: Branch protection rules
**By:** Toru (Architect)
**What:** `main` branch: required status checks (backend build+test, frontend build+test), required PR with 1 approval, no direct push. `dev` branch: required status checks (build+test pass), direct push allowed for squad agents (to unblock flow during PoC). Squad feature branches follow `squad/{slug}` → PR → `dev`.
**Why:** `main` is protected to prevent broken production deployments. `dev` protection ensures tests pass before integration but allows direct push for squad agility during PoC phase.

### 2026-05-23: CI pipeline steps — build, test, migrate, deploy
**By:** Toru (Architect)
**What:** Backend pipeline steps: (1) `dotnet restore`, (2) `dotnet build --no-restore`, (3) `dotnet test --no-build --collect:"XPlat Code Coverage"`, (4) `dotnet publish -c Release -o ./publish`, (5) `dotnet ef database update` (dev only — prod migrations are script-based review), (6) `az webapp deploy`. Functions pipeline mirrors steps 1–4 then `az functionapp deployment source config-zip`.
**Why:** EF migrations run automatically on dev for speed. On prod, a reviewed SQL script is applied manually (or via deployment slot swap) to prevent accidental schema destruction. Code coverage collected for visibility without blocking the pipeline.

---

### 2026-05-23: Data Model Approved

**By:** Toru (Architect)
**What:** Reviewed submitted C# data model (Product, ProductCategory, Customer, SalesOrder, SalesOrderDetail, ProductInventory). Approved with required additions: `Product.IsActive` (bool, soft-delete), `ProductCategory.IsActive` (bool, soft-delete), `Customer.UserId` (string, FK to AspNetUsers.Id for role-based auth), `SalesOrder.Status` (string, order management/tracking), `SalesOrder.PaymentStatus` (string, payment simulation feature), `ProductInventory.LastUpdated` (DateTime, StockUpdateQueue Function audit trail), `ProductInventory.ReorderThreshold` (int, stock alert threshold). `SalesOrderDetail` approved as-is. Cart is client-side only — no Cart or CartItem entities in the DB (ADR-004 confirmed). C# class names to be aligned to established DB table names via EF `ToTable()` configuration (SalesOrder→Orders, SalesOrderDetail→OrderItems, ProductCategory→Categories, ProductInventory→Inventory).
**Why:** Additions are the strict minimum required to support all 8 confirmed feature requirements. No over-engineering. Client-side cart eliminates two entities with zero feature loss.

---

### 2026-05-23: Adventure Works-inspired schema — six core tables
**By:** Toru (Architect)
**What:** Schema has six application tables: `Categories`, `Products`, `Customers`, `Orders`, `OrderItems`, `Inventory`. Identity (users/roles) uses ASP.NET Core Identity default tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). `Customers.UserId` is a FK to `AspNetUsers.Id`.
**Why:** Separates authentication identity (ASP.NET Identity) from business domain (Customers). A Customer record is created upon registration and linked to the Identity user. This pattern aligns with Adventure Works's Person/Customer split.

### 2026-05-23: EF Core with repository pattern via OutdoorsShop.Infrastructure
**By:** Toru (Architect)
**What:** EF Core 10 is the ORM. DbContext lives in `OutdoorsShop.Infrastructure`. Repositories implement interfaces defined in `OutdoorsShop.Core`. No raw ADO.NET in application code. Migrations managed via EF Core CLI (`dotnet ef migrations`).
**Why:** Repository pattern decouples controllers from the ORM, making unit testing with in-memory providers or mocks straightforward. Cinnamon owns implementation; interfaces in Core enforce the contract.

### 2026-05-23: Soft deletes via IsActive flag on Products and Categories
**By:** Toru (Architect)
**What:** `Products.IsActive` and `Categories.IsActive` columns implement logical delete. Physical DELETE is not used for these tables. All queries filter `WHERE IsActive = 1` by default via EF Core global query filters.
**Why:** Product removal from catalog must not break historical order records that reference the product. Soft delete preserves referential integrity.

### 2026-05-23: Inventory table ReorderThreshold for stock alerts
**By:** Toru (Architect)
**What:** `Inventory.ReorderThreshold` is an INT column. When the `StockUpdateQueue` function reduces quantity, it checks if `Quantity <= ReorderThreshold` and can raise an alert (logged to App Insights for PoC; extensible to email later).
**Why:** Business requirement: inventory tracking. Threshold makes the stock-update function actionable without hard-coded logic.

---

### 2026-05-23: Monorepo folder structure adopted
**By:** Toru (Architect)
**What:** Single Git repository with three top-level source areas: `src/` (.NET projects), `frontend/` (React + TypeScript), and `infra/` (Bicep IaC). A shared `OutdoorsShop.sln` ties all .NET projects together. Azure Functions live in `src/OutdoorsShop.Functions/` as an isolated-process project.
**Why:** Keeps the full stack version-aligned in one repo. Simplifies cross-concern refactors and CI/CD. Avoids submodule complexity for a single-team PoC.

### 2026-05-23: .NET project layering (Api / Core / Infrastructure / Functions / Tests)
**By:** Toru (Architect)
**What:** Five .NET projects: `OutdoorsShop.Api` (controllers, middleware, startup), `OutdoorsShop.Core` (domain entities, interfaces — no dependencies), `OutdoorsShop.Infrastructure` (EF Core, repositories, storage clients), `OutdoorsShop.Functions` (Azure Functions isolated), `OutdoorsShop.Tests` (xUnit).
**Why:** Classic onion/clean separation. Core has zero external dependencies, making unit testing trivial. Infrastructure is the only layer that touches EF Core and Azure SDKs.

### 2026-05-23: React app scaffolded with Vite + TypeScript
**By:** Toru (Architect)
**What:** React frontend lives in `frontend/` and is scaffolded with `npm create vite@latest -- --template react-ts`. Build output goes to `frontend/dist/`.
**Why:** Vite is the current standard React toolchain. Fast HMR for development, clean static output for deployment.

---

### 2026-05-23: EF Core InitialCreate Migration Applied

**By:** Cinnamon (Backend Developer)
**What:** Created EF Core initial migration (InitialCreate) targeting OutdoorsShopDB on Azure SQL (azure-sql-pampa.database.windows.net). Migration files in src/OutdoorsShop.Infrastructure/Data/Migrations/. Connection string stored in .NET User Secrets for the Api project — never committed. Tables defined: Products, Categories, Customers, Orders, OrderItems, Inventory + ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, AspNetUserRoles, etc.).
**Why:** Database schema must be applied before any API endpoints can be tested end-to-end.
**Status:** Migration CREATED and committed. `database update` BLOCKED — ShopAdmin user lacks CREATE TABLE permission on OutdoorsShopDB. Needs `ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;` (or `db_owner`) executed by the Azure SQL server admin before schema can be applied.
**Fix required:** Connect to azure-sql-pampa.database.windows.net as server admin and run:
```sql
USE OutdoorsShopDB;
ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;
-- or for full ownership:
ALTER ROLE db_owner ADD MEMBER ShopAdmin;
```
Then re-run: `dotnet ef database update --project src/OutdoorsShop.Infrastructure --startup-project src/OutdoorsShop.Api`
**Also fixed:** AppDbContext.OnModelCreating — added explicit HasKey() for ProductCategory.CategoryID, SalesOrder.OrderID, SalesOrderDetail.OrderDetailID (non-conventional PK names not auto-detected by EF Core convention).

---

### 2026-05-23: Database Schema Applied to Azure SQL

**By:** Jorgito (confirmed) / Cinnamon (migration)
**What:** dotnet ef database update completed successfully (exit code 0). All tables created on OutdoorsShopDB at azure-sql-pampa.database.windows.net: Products, Categories, Customers, Orders, OrderItems, Inventory + full ASP.NET Core Identity schema (AspNetUsers, AspNetRoles, AspNetUserClaims, AspNetUserTokens, AspNetRoleClaims, AspNetUserRoles). ShopAdmin granted db_owner role by Jorgito via Azure portal.
**Why:** Database schema must exist before API endpoints can be tested end-to-end. Milestone: backend + database are fully connected.

---

### 2026-05-23: Products and Categories API Implemented

**By:** Cinnamon (Backend Developer)
**What:** Full CRUD for Products and Categories. GET endpoints are public (AllowAnonymous). POST/PUT/DELETE require Administrator role. Soft-delete pattern (IsActive=false) for both entities. ProductRepository now eagerly loads Category on all queries. Creating a product automatically creates a ProductInventory record (qty=0, threshold=5). Categories seeded: Camping, Trekking, Cycling, Climbing. Global query filters exclude inactive records transparently.
**Why:** First API endpoints — unblocks Malta's catalog page and Creta's integration tests.

---

## Governance

- All meaningful architectural changes require Toru's approval
- Document architectural decisions here via the inbox drop-box pattern
- Keep history focused on work, decisions focused on direction
